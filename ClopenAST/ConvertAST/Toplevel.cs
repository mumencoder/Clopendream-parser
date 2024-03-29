﻿using OpenDreamShared.Dream;
using DMCompiler.Compiler.DM;

namespace ClopenDream {
    public partial class ConvertAST {

        public ConvertAST() { }

        public DMASTFile GetFile(Node root) {
            pathStack = new();
            return new DMASTFile(root.Location, GetBlockInner(root.Leaves));
        }

        DMASTBlockInner GetBlockInner(List<Node> nodes) {
            List<DMASTStatement> stmts = new();
            foreach (var leaf in nodes) {
                if (_verbose) { Console.WriteLine($"GetBlockInner {leaf.Print()}"); }
                stmts.AddRange(GetStatements(leaf));
            }
            return new DMASTBlockInner(nodes.Count > 0 ? nodes[0].Location : OpenDreamShared.Compiler.Location.Unknown, stmts.ToArray());
        }
        IEnumerable<DMASTStatement> GetStatements(Node node) {
            if (node.Labels.Contains("ObjectDecl")) {
                pathStack.Push(node);
                yield return new DMASTObjectDefinition(node.Location, ExtractPath(pathStack), GetBlockInner(node.Leaves));
                pathStack.Pop();
            }
            else if (node.Labels.Contains("ObjectVarDecl")) {
                foreach (var leaf in node.Leaves) {
                    pathStack.Push(node);
                    foreach (var stmt in GetObjectVarDefinitions(leaf)) {
                        yield return stmt;
                    }
                    pathStack.Pop();
                }
            }
            else if (node.Labels.Contains("ProcDecl")) {
                pathStack.Push(node);
                foreach (var leaf in node.Leaves) {
                    yield return GetProcDecl(leaf, pathStack);
                }
                pathStack.Pop();
            }
            else if (node.Labels.Contains("ProcOverride")) {
                yield return GetProcDecl(node, pathStack);
            }
            else if (node.Labels.Contains("ObjectAssignStmt")) {
                pathStack.Push(node.Leaves[0]);
                var define = new DMASTObjectVarOverride(node.Location, ExtractPath(pathStack), GetExpression(node.Leaves[1]));
                AssociateNodes(node, define);
                VisitDefine(node, define);
                yield return define;
                pathStack.Pop();
            }
            else if (node.Labels.Contains("ParentDecl")) {
                // todo: this goes somewhere not in the AST
            }
            else if (node.Labels.Contains("ChildDecl")) {
                // todo: this goes somewhere not in the AST
            }
            else {
                throw node.Error("GetStatements");
            }
        }

        DMASTProcDefinition GetProcDecl(Node n, Stack<Node> pathStack) {
            pathStack.Push(n);
            var path = ExtractPath(pathStack);
            ProcNode = n;
            var body = GetProcBlockInner(n.Leaves.Skip(1).ToList());
            var procdef = new DMASTProcDefinition(n.Location, path, GetProcParameters(n.Leaves[0]), body);
            AssociateNodes(n, procdef);
            VisitDefine(n, procdef);
            pathStack.Pop();
            return procdef;
        }

        DMASTDefinitionParameter[] GetProcParameters(Node n) {
            if (!n.Labels.Contains("ProcHeader")) {
                throw n.Error("GetProcParameters");
            }
            var arg_group = n.Leaves[0];
            if (!arg_group.Labels.Contains("ArgGroup")) {
                throw n.Error("GetProcParameters");
            }
            List<DMASTDefinitionParameter> result = new();
            foreach (var arg_node in arg_group.Leaves) {
                result.Add(GetProcParameter(arg_node));
            }
            return result.ToArray();
        }

        DMASTDefinitionParameter GetProcParameter(Node n) {
            Node term_node;
            DMASTExpression varinit_expr = null;
            var path = ExtractPath(GetPath(n, out term_node));
            var val_type = OpenDreamShared.Dream.Procs.DMValueType.Anything;
            DMASTExpression possible_vals = null;
            if (term_node.Leaves.Count > 0) {
                if (term_node.Leaves.Count > 1) {
                    throw n.Error("GetProcParameter");
                }
                term_node = term_node.Leaves[0];
                foreach (var leaf in term_node.Leaves) {
                    if (leaf.Labels.Contains("VarInit")) {
                        varinit_expr = GetVarInit(leaf);
                    }
                    else if (leaf.Labels.Contains("AsModifier")) {
                        val_type = ConvertDMValueType(leaf.Leaves[0]);
                    }
                    else if (leaf.Labels.Contains("InModifier")) {
                        possible_vals = GetExpression(leaf.Leaves[0]);
                    }
                    else {
                        throw leaf.Error("GetProcParameter");
                    }
                }
            }
            return new DMASTDefinitionParameter(n.Location, new DMASTPath(n.Location, path), varinit_expr, val_type, possible_vals);
        }

        DMASTProcBlockInner GetProcBlockInner(List<Node> nodes) {
            return new DMASTProcBlockInner(nodes.Count > 0 ? nodes[0].Location : OpenDreamShared.Compiler.Location.Unknown, GetProcStatements(nodes).ToArray());
        }

        DMASTExpression GetVarInit(Node node) {
            DMASTExpression expr = null;
            if (node != null && node.Labels.Contains("VarInit")) {
                expr = GetExpression(node.Leaves[0]);
            }
            else {
                throw node.Error("GetVarInit");
            }
            return expr;
        }

        Stack<Node> GetPath(Node n, out Node term_node) {
            Node cnode = n;
            Stack<Node> path = new();
            path.Push(cnode);
            while (!cnode.Labels.Contains("PathTerminated")) {
                if (cnode.Leaves.Count > 1) {
                    throw n.Error("GetPath");
                }
                cnode = cnode.Leaves[0];
                path.Push(cnode);
            }
            term_node = cnode;
            return path;
        }
        IEnumerable<DMASTObjectVarDefinition> GetObjectVarDefinitions(Node node) {
            if (node.Labels.Contains("PathTerminated")) {
                pathStack.Push(node);
                DMASTExpression expr = new DMASTConstantNull(node.Location);
                var val_type = OpenDreamShared.Dream.Procs.DMValueType.Anything;
                var index_modifier = false;
                DMASTExpression array_size = null;
                foreach (var subnode in node.Leaves) {
                    var modnode = subnode.UniqueLeaf();
                    if (modnode == null) {
                        throw node.Error("null modifier");
                    }
                    if (modnode.Labels.Contains("VarInit")) {
                        expr = GetExpression(modnode.Leaves[0]);
                    }
                    // TODO handle a sized array correctly
                    else if (modnode.Labels.Contains("IndexModifier")) {
                        index_modifier = true;
                        if (modnode.Leaves.Count > 0) {
                            array_size = GetExpression(modnode.Leaves[0]);
                        }
                    }
                    else if (modnode.Labels.Contains("AsModifier")) {
                        val_type = ConvertDMValueType(modnode.Leaves[0]);
                    }
                    else if (modnode.Labels.Contains("InModifier")) {
                        // TODO: this might be ignored
                    }
                    else {
                        throw node.Error("Unknown object var declaration modifier");
                    }

                }
                var dpath = ExtractPath(pathStack);
                if (index_modifier) {
                    if (dpath.FindElement("list") == -1 && dpath.FindElement("var") != -1) {
                        int? var_pos = null;
                        while (dpath.FindElement("var") != -1) {
                            if (var_pos == null) { var_pos = dpath.FindElement("var"); }
                            dpath = dpath.RemoveElement(dpath.FindElement("var"));
                        }
                        var l_path = dpath.FromElements(0, var_pos.Value);
                        l_path.Type = dpath.Type;

                        var r_path = dpath.FromElements(var_pos.Value);
                        r_path.Type = DreamPath.PathType.Relative;

                        dpath = l_path.Combine( new DreamPath($"var/list")).Combine( r_path );
                    }
                    if (array_size != null) {
                        var paras = new DMASTCallParameter[1];
                        paras[0] = new DMASTCallParameter(node.Location, array_size);
                        expr = new DMASTNewPath(node.Location, new DMASTPath(node.Location, new OpenDreamShared.Dream.DreamPath("/list")), paras);
                    }
                }
                var define = new DMASTObjectVarDefinition(node.Location, dpath, expr, valType:val_type);
                AssociateNodes(node, define);
                VisitDefine(node, define);
                yield return define;
                pathStack.Pop();
            }
            else if (node.Labels.Contains("PathDecl")) {
                pathStack.Push(node);
                foreach (var leaf in node.Leaves) {
                    foreach (var define in GetObjectVarDefinitions(leaf)) {
                        yield return define;
                    };
                }
                pathStack.Pop();
            }
            else {
                throw node.Error("GetObjectVarDefinitions");
            }
        }
    }
}