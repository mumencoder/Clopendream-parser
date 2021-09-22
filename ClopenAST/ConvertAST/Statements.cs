using System;
using System.Linq;
using System.Collections.Generic;
using OpenDreamShared.Compiler.DM;

namespace DMTreeParse {
    public partial class ConvertAST {

        Node DebugStatementNode = null;

        IEnumerable<DMASTProcStatement> GetProcStatements(List<Node> nodes) {
            int cnode = 0;
            while (cnode < nodes.Count) {
                var node = nodes[cnode];
                DebugStatementNode = node;

                if (node.Labels.Contains("IfStmt")) {
                    cnode += 1;
                    var ifbody = node.Next();
                    DMASTProcBlockInner if_block = null, else_block = null;
                    if (ifbody != null && ifbody.Labels.Contains("IfBody")) {
                        cnode += 1;
                        if_block = GetProcBlockInner(ifbody.Leaves);
                    }
                    var elsebody = ifbody.Next();
                    if (elsebody != null && elsebody.Labels.Contains("ElseBody")) {
                        cnode += 1;
                        else_block = GetProcBlockInner(elsebody.Leaves);
                    }
                    yield return new DMASTProcStatementIf(GetExpression(node.Leaves[0]), if_block, else_block);

                }
                else if (node.Labels.Contains("SwitchStmt")) {
                    cnode += 1;
                    var switchbody = node.Next();
                    List<DMASTProcStatementSwitch.SwitchCase> cases = new();
                    if (switchbody != null && switchbody.Labels.Contains("SwitchBody")) {
                        cnode += 1;
                        var switch_cnode = 0;
                        while (switch_cnode < switchbody.Leaves.Count) {
                            if (switchbody.Leaves[switch_cnode].Labels.Contains("SwitchIfHeader")) {
                                var switch_if_header = switchbody.Leaves[switch_cnode];
                                var switch_if_body = switchbody.Leaves[switch_cnode + 1];
                                if (switch_if_header.Leaves[0].Tags.ContainsKey("blank")) {
                                    var maybe_to = switch_if_header.Leaves[0].IgnoreBlank();
                                    if (maybe_to.CheckTag("operator", "to")) {
                                        var switch_range = new DMASTSwitchCaseRange(GetExpression(maybe_to.Leaves[0]), GetExpression(maybe_to.Leaves[1]));
                                        cases.Add(new DMASTProcStatementSwitch.SwitchCaseValues(new DMASTExpression[1] { switch_range }, GetProcBlockInner(switch_if_body.Leaves)));
                                        switch_cnode += 2;
                                        continue;
                                    }
                                }
                                List<DMASTExpression> values = switch_if_header.Leaves.Select((n) => GetExpression(n)).ToList();
                                switch_cnode += 1;
                                if (switchbody.Leaves[switch_cnode].Labels.Contains("SwitchIfBody")) {
                                    switch_if_body = switchbody.Leaves[switch_cnode];
                                    switch_cnode += 1;
                                }
                                cases.Add(new DMASTProcStatementSwitch.SwitchCaseValues(values.ToArray(), GetProcBlockInner(switch_if_body.Leaves)));
                            }
                            else if (switchbody.Leaves[switch_cnode].Labels.Contains("SwitchElseBody")) {
                                cases.Add(new DMASTProcStatementSwitch.SwitchCaseDefault(GetProcBlockInner(switchbody.Leaves[switch_cnode].Leaves)));
                                switch_cnode += 1;
                            }
                            else {
                                throw switchbody.Error("SwitchStmt");
                            }
                        }
                    }
                    yield return new DMASTProcStatementSwitch(GetExpression(node.Leaves[0]), cases.ToArray());
                }
                else if (node.Labels.Contains("SpawnStmt")) {
                    cnode += 1;
                    DMASTExpression expr = new DMASTConstantInteger(0);
                    DMASTProcBlockInner body = null;
                    if (node.Leaves.Count > 0 && node.Leaves.Count != 1) {
                        if (node.Leaves.Count != 1) {
                            throw node.Error("unexpected spawn expr");
                        }
                        expr = GetExpression(node.Leaves[0]);
                    }
                    var spawnbody = node.Next();
                    if (spawnbody != null && spawnbody.Labels.Contains("SpawnBody")) {
                        cnode += 1;
                        body = GetProcBlockInner(spawnbody.Leaves);
                    }
                    yield return new DMASTProcStatementSpawn(expr, body);
                }
                else if (node.Labels.Contains("ReturnStmt")) {
                    cnode += 1;
                    DMASTExpression expr = null;
                    if (node.Leaves.Count == 0) {
                        expr = ExprInnerToNode(node);
                    }
                    if (node.Leaves.Count == 1) {
                        expr = GetExpression(node.Leaves[0]);
                    }
                    yield return new DMASTProcStatementReturn(expr);
                }
                else if (node.Labels.Contains("ProcVarDeclStmt")) {
                    var (for_stmt, new_cnode) = InitializedForLoop(nodes, cnode);
                    if (for_stmt != null) {
                        yield return for_stmt;
                        cnode = new_cnode;
                    }
                    else {
                        cnode += 1;
                        foreach (var stmt in GetProcVarDefinitions(node)) {
                            yield return stmt;
                        }
                    }
                }
                else if (node.Labels.Contains("ForStmt")) {
                    var (stmt, new_cnode) = ForLoop(nodes, cnode);
                    if (stmt != null) {
                        yield return stmt;
                        cnode = new_cnode;
                    }
                    else {
                        throw node.Error("ForStmt");
                    }
                }
                else if (node.Labels.Contains("WhileStmt")) {
                    cnode += 1;
                    DMASTExpression cond = GetExpression(node.Leaves[0]);

                    var while_body_node = node.Next();
                    DMASTProcBlockInner while_body = null;
                    if (while_body_node != null && while_body_node.Labels.Contains("WhileBody")) {
                        cnode += 1;
                        while_body = GetProcBlockInner(while_body_node.Leaves);
                    }
                    yield return new DMASTProcStatementWhile(cond, while_body);
                }
                else if (node.Labels.Contains("DoWhileStmt")) {
                    cnode += 1;
                    DMASTProcBlockInner while_body = GetProcBlockInner(node.Leaves);

                    var while_cond_node = node.Next();
                    DMASTExpression while_cond = null;
                    if (while_cond_node != null && while_cond_node.Labels.Contains("DoWhileEnd")) {
                        cnode += 1;
                        while_cond = GetExpression(while_cond_node.Leaves[0]);
                    }
                    yield return new DMASTProcStatementDoWhile(while_cond, while_body);
                }
                else if (node.Labels.Contains("SetStmt")) {
                    cnode += 1;
                    var ident = (DMASTIdentifier)GetExpression(node.Leaves[0].Leaves[0]);
                    yield return new DMASTProcStatementSet(ident.Identifier, GetExpression(node.Leaves[0].Leaves[1]));
                }
                else if (node.Labels.Contains("ContinueStmt")) {
                    cnode += 1;
                    yield return new DMASTProcStatementContinue();
                }
                else if (node.Labels.Contains("BreakStmt")) {
                    cnode += 1;
                    yield return new DMASTProcStatementBreak();
                }
                else if (node.Labels.Contains("ExplicitBlock")) {
                    cnode += 1;
                    foreach (var stmt in GetProcStatements(node.Leaves)) {
                        yield return stmt;
                    }
                }
                else {
                    cnode += 1;
                    if (node.Labels.Contains("OperatorExpression")) {
                        string op = (string)node.Tags["operator"];
                        if (op == "<<") {
                            var rnode = node.Leaves[1];
                            if (rnode.Tags.ContainsKey("blank") && rnode.Leaves.Count == 1) {
                                var browse_node = rnode.Leaves[0];
                                if (browse_node.Tags.ContainsKey("bare") && browse_node.Tags["bare"] as string == "browse") {
                                    yield return new DMASTProcStatementBrowse(GetExpression(node.Leaves[0]), GetExpression(browse_node.Leaves[0]), GetExpression(browse_node.Leaves[1]));
                                    continue;
                                }
                                else if (browse_node.Tags.ContainsKey("bare") && browse_node.Tags["bare"] as string == "browse_rsc") {
                                    yield return new DMASTProcStatementBrowseResource(GetExpression(node.Leaves[0]), GetExpression(browse_node.Leaves[0]), GetExpression(browse_node.Leaves[1]));
                                    continue;
                                }
                                else if (browse_node.Tags.ContainsKey("bare") && browse_node.Tags["bare"] as string == "output") {
                                    yield return new DMASTProcStatementOutputControl(GetExpression(node.Leaves[0]), GetExpression(browse_node.Leaves[0]), GetExpression(browse_node.Leaves[1]));
                                    continue;
                                }
                            }
                        }
                    }
                    if (node.Labels.Contains("BuiltinExpression")) {
                        string proc_ident = (string)node.Tags["bare"];
                        if (proc_ident == "del") {
                            yield return new DMASTProcStatementDel(GetExpression(node.Leaves[0]));
                            continue;
                        }
                    }
                    yield return new DMASTProcStatementExpression(GetExpression(node));
                }
            }
        }

        Stack<Node> procVarPathStack = new();
        IEnumerable<DMASTProcStatementVarDeclaration> GetProcVarDefinitions(Node node) {
            if (node.Labels.Contains("PathTerminated")) {
                procVarPathStack.Push(node);
                Node expr_node = node.UniqueBlank();
                DMASTExpression expr = null;
                if (expr_node != null && expr_node.Labels.Contains("VarInit")) {
                    expr = GetExpression(expr_node.Leaves[0]);
                }
                var path = new DMASTPath(ExtractPath(procVarPathStack));
                yield return new DMASTProcStatementVarDeclaration(path, expr);
                procVarPathStack.Pop();
            }
            else if (node.Labels.Contains("PathDecl") || node.Labels.Contains("ProcVarDeclStmt")) {
                procVarPathStack.Push(node);
                foreach (var leaf in node.Leaves) {
                    foreach (var define in GetProcVarDefinitions(leaf)) {
                        yield return define;
                    };
                }
                procVarPathStack.Pop();
            }
            else {
                throw node.Error("GetProcVarDefinitions");
            }
        }

        string FindUniqueDeclareName(Node node) {
            while (!node.Labels.Contains("PathTerminated")) {
                if (node.Leaves.Count != 1) {
                    return null;
                }
                node = node.Leaves[0];
            }
            return node.Tags["bare"] as string;
        }
        (DMASTProcStatement, int) InitializedForLoop(List<Node> nodes, int cnode) {
            var decl_node = nodes[cnode];
            if (decl_node.Labels.Contains("ProcVarDeclStmt")) {
                var decl_name = FindUniqueDeclareName(decl_node);
                cnode += 1;
                if (decl_node == null) {
                    return (null, 0);
                }
                var initializer = GetProcVarDefinitions(decl_node).ToArray()[0];
                return ForLoop(nodes, cnode, initializer, decl_name); 
            }
            return (null, 0);
        }
        (DMASTProcStatement, int) ForLoop(List<Node> nodes, int cnode, DMASTProcStatementVarDeclaration initializer = null, string decl_name = null) {
            if (cnode >= nodes.Count) {
                return (null, 0);
            }
            var for_node = nodes[cnode];
            if (for_node.Labels.Contains("ForStmt")) {
                cnode += 1;
                if (for_node.Leaves.Count == 3) {
                    DMASTProcStatement init = null;
                    DMASTExpression compa = null;
                    DMASTExpression incr = null;
                    init = GetProcStatements(for_node.Leaves).ToList()[0];
                    compa = GetExpression(for_node.Leaves[1]);
                    incr = GetExpression(for_node.Leaves[2]);
                    var for_body_node = for_node.Next();
                    DMASTProcBlockInner for_body = null;
                    init = FixInitializer(initializer, init);
                    if (for_body_node != null && for_body_node.Labels.Contains("ForBody")) {
                        cnode += 1;
                        for_body = GetProcBlockInner(for_body_node.Leaves);
                    }
                    return (new DMASTProcStatementForStandard(init, compa, incr, for_body), cnode);
                }
                if (for_node.Leaves.Count == 1) {
                    var for_expr = for_node.Leaves[0].IgnoreBlank();
                    if (for_expr.CheckTag("operator", "in") || for_expr.CheckTag("operator", "=")) {
                        var for_mod = for_expr.Leaves[0].IgnoreBlank();
                        string[] for_name = null;
                        var dm_type = OpenDreamShared.Dream.Procs.DMValueType.Anything;
                        if (for_mod.CheckTag("operator", "as")) {
                            for_name = for_mod.Leaves[0].Tags["ident"] as string[];
                            dm_type = ConvertDMValueType(for_mod.Leaves[1]);
                            if (dm_type != 0) {
                                throw for_mod.Error("yep hello");
                            }
                        }
                        else {
                            for_name = for_mod.Tags["ident"] as string[];
                        }
                        if (decl_name != null && for_name[0] != decl_name) {
                            return (null, 0);
                        }
                        var body_node = for_node.Next();
                        DMASTProcBlockInner body = null;
                        if (body_node != null && body_node.Labels.Contains("ForBody")) {
                            cnode += 1;
                            body = GetProcBlockInner(body_node.Leaves);
                        }
                        if (for_expr.Leaves[1].Tags.ContainsKey("blank")) {
                            var to_expr = for_expr.Leaves[1].IgnoreBlank();
                            if (to_expr.CheckTag("operator", "to")) {
                                var range_start = GetExpression(to_expr.Leaves[0]);
                                var range_end = GetExpression(to_expr.Leaves[1]);
                                DMASTExpression range_step = new DMASTConstantInteger(1);
                                if (to_expr.Leaves.Count == 3) {
                                    range_step = GetExpression(to_expr.Leaves[2]);
                                }
                                if (for_expr.CheckTag("operator", "=")) {
                                    initializer.Value = range_start;
                                }
                                return (new DMASTProcStatementForRange(initializer, new DMASTIdentifier(for_name[0]),
                                    range_start, range_end, range_step, body), cnode);
                            }
                            else {
                                return (new DMASTProcStatementForList(initializer, new DMASTIdentifier(for_name[0]),
                                    GetExpression(for_expr.Leaves[1]), body), cnode);
                            }
                        }
                        var list_expr = GetExpression(for_expr.Leaves[1]);
                        return (new DMASTProcStatementForList(initializer, new DMASTIdentifier(for_name[0]), list_expr, body), cnode);
                    }
                }
            }
            return (null, 0);
        }
        DMASTProcStatement FixInitializer(DMASTProcStatementVarDeclaration initializer, DMASTProcStatement init) {
            if (initializer == null) {
                return init;
            }
            if (init is DMASTProcStatementExpression expr) {
                if (expr.Expression is DMASTAssign nassign) {
                    if (nassign.Expression is DMASTIdentifier id) {
                        if (initializer.Name == id.Identifier) {
                            initializer.Value = nassign.Value;
                            return initializer;
                        }
                    }
                }
            }
            return init;
        }
    }
}