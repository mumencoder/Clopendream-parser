using System;
using System.Linq;
using System.Collections.Generic;
using OpenDreamShared.Dream;
using OpenDreamShared.Compiler.DM;

namespace ClopenDream {
    public partial class ConvertAST {

        public ConvertAST() { }

        public DMASTFile GetFile(Node root) {
            pathStack = new();
            return new DMASTFile(GetBlockInner(root.Leaves));
        }

        DMASTBlockInner GetBlockInner(List<Node> nodes) {
            List<DMASTStatement> stmts = new();
            foreach (var leaf in nodes) {
                stmts.AddRange(GetStatements(leaf));
            }
            return new DMASTBlockInner(stmts.ToArray());
        }
        IEnumerable<DMASTStatement> GetStatements(Node node) {
            if (node.Labels.Contains("ObjectDecl")) {
                pathStack.Push(node);
                yield return new DMASTObjectDefinition(ExtractPath(pathStack), GetBlockInner(node.Leaves));
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
                var define = new DMASTObjectVarOverride(ExtractPath(pathStack), GetExpression(node.Leaves[1]));
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
//            Console.WriteLine("GetProcDecl \"" + path + "\"");
            ProcNode = n;
            var body = GetProcBlockInner(n.Leaves.Skip(1).ToList());
            if (n.Leaves.Count == 1) {
                body = null;
            }
            var procdef = new DMASTProcDefinition(path, GetProcParameters(n.Leaves[0]), body);
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
            return new DMASTDefinitionParameter(new DMASTPath(path), varinit_expr, val_type, possible_vals);
        }

        DMASTProcBlockInner GetProcBlockInner(List<Node> nodes) {
            return new DMASTProcBlockInner(GetProcStatements(nodes).ToArray());
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
                Node expr_node = node.UniqueBlank();
                DMASTExpression expr = new DMASTConstantNull();
                if (expr_node != null && expr_node.Labels.Contains("VarInit")) {
                    expr = GetExpression(expr_node.Leaves[0]);
                }
                var define = new DMASTObjectVarDefinition(ExtractPath(pathStack), expr);
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