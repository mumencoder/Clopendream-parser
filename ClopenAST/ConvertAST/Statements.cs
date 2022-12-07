using System.Linq;
using System.Collections.Generic;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace ClopenDream {
    public partial class ConvertAST {
        IEnumerable<DMASTProcStatement> GetProcStatements(List<Node> nodes) {
            int cnode = 0;
            while (cnode < nodes.Count) {
                var node = nodes[cnode];

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
                    yield return new DMASTProcStatementIf(node.Location, GetExpression(node.Leaves[0]), if_block, else_block);

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
                                switch_cnode += 1;
                                List<DMASTExpression> switch_case_values = new();
                                foreach(var switch_leaf in switch_if_header.Leaves) {
                                    if (switch_leaf.Tags.ContainsKey("blank")) {
                                        var maybe_to = switch_if_header.Leaves[0].IgnoreBlank();
                                        if (maybe_to.CheckTag("operator", "to")) {
                                            switch_case_values.Add( new DMASTSwitchCaseRange(node.Location, GetExpression(maybe_to.Leaves[0]), GetExpression(maybe_to.Leaves[1])));
                                        } else {
                                            switch_case_values.Add( GetExpression(switch_leaf));
                                        }
                                    } else {
                                        switch_case_values.Add(GetExpression(switch_leaf));
                                    }
                                }
                                if (switchbody.Leaves[switch_cnode].Labels.Contains("SwitchIfBody")) {
                                    switch_if_body = switchbody.Leaves[switch_cnode];
                                    switch_cnode += 1;
                                }
                                cases.Add(new DMASTProcStatementSwitch.SwitchCaseValues(switch_case_values.ToArray(), GetProcBlockInner(switch_if_body.Leaves)));
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
                    yield return new DMASTProcStatementSwitch(node.Location, GetExpression(node.Leaves[0]), cases.ToArray());
                }
                else if (node.Labels.Contains("SpawnStmt")) {
                    cnode += 1;
                    DMASTExpression expr = new DMASTConstantInteger(node.Location, 0);
                    DMASTProcBlockInner body = null;
                    if (node.Leaves.Count > 0) {
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
                    yield return new DMASTProcStatementSpawn(node.Location, expr, body);
                }
                else if (node.Labels.Contains("TryStmt")) {
                    DMASTProcBlockInner trybody = null;
                    DMASTProcBlockInner catchbody = null;
                    DMASTProcStatement catchexpr = null;
                    if (node.Leaves.Count > 0) {
                        trybody = GetProcBlockInner(node.Leaves);
                    }
                    cnode += 1;
                    var catchnode = node.Next();
                    if (catchnode.Labels.Contains("TryVarDecl")) {
                        cnode += 1;
                        foreach (var stmt in GetProcStatements(new() { catchnode })) {
                            catchexpr = stmt;
                        }
                        catchnode = catchnode.Next();
                    }
                    if (catchnode == null || !catchnode.Labels.Contains("CatchStmt")) {
                        throw catchnode.Error("expected CatchStmt");
                    }
                    cnode += 1;
                    catchnode = catchnode.Next();
                    if (catchnode == null || !catchnode.Labels.Contains("CatchBody")) {
                        throw catchnode.Error("expected CatchBody");
                    }
                    cnode += 1;
                    catchbody = GetProcBlockInner(catchnode.Leaves);
                    yield return new DMASTProcStatementTryCatch(node.Location, trybody, catchbody, catchexpr);

                }
                else if (node.Labels.Contains("ThrowStmt")) {
                    cnode += 1;
                    DMASTExpression expr = null;
                    if (node.Leaves.Count > 0) {
                        expr = GetExpression(node.Leaves[0]);
                    }
                    yield return new DMASTProcStatementThrow(node.Location, expr);
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
                    yield return new DMASTProcStatementReturn(node.Location, expr);
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
                    yield return new DMASTProcStatementWhile(node.Location, cond, while_body);
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
                    yield return new DMASTProcStatementDoWhile(node.Location, while_cond, while_body);
                }
                else if (node.Labels.Contains("SetStmt")) {
                    cnode += 1;
                    var ident = (DMASTIdentifier)GetExpression(node.Leaves[0].Leaves[0]);
                    yield return new DMASTProcStatementSet(node.Location, ident.Identifier, GetExpression(node.Leaves[0].Leaves[1]));
                }
                else if (node.Labels.Contains("ContinueStmt")) {
                    cnode += 1;
                    DMASTIdentifier label = null;
                    if (node.Tags.ContainsKey("deref")) {
                        string s = (node.Tags["deref"] as string[])[0];
                        label = new DMASTIdentifier(node.Location, s);
                    }
                    yield return new DMASTProcStatementContinue(node.Location, label: label);
                }
                else if (node.Labels.Contains("BreakStmt")) {
                    cnode += 1;
                    DMASTIdentifier label = null;
                    if (node.Tags.ContainsKey("deref")) {
                        string s = (node.Tags["deref"] as string[])[0];
                        label = new DMASTIdentifier(node.Location, s);
                    }
                    yield return new DMASTProcStatementBreak(node.Location, label: label);
                }
                else if (node.Labels.Contains("GotoStmt")) {
                    cnode += 1;
                    string[] labels = node.Tags["deref"] as string[];
                    yield return new DMASTProcStatementGoto(node.Location, new DMASTIdentifier(node.Location, labels[0]));
                }
                else if (node.Labels.Contains("LabeledBlock")) {
                    cnode += 1;
                    yield return new DMASTProcStatementLabel(node.Location, node.Tags["block"] as string, GetProcBlockInner(node.Leaves));
                }
                else if (node.Labels.Contains("ExplicitBlock")) {
                    cnode += 1;
                    foreach (var stmt in GetProcStatements(node.Leaves)) {
                        yield return stmt;
                    }
                }
                else if (node.Labels.Contains("ImplicitBlock")) {
                    cnode += 1;
                    yield return new DMASTProcStatementLabel(node.Location, node.Tags["block"] as string, GetProcBlockInner(node.Leaves));
                }
                else if (node.Labels.Contains("ParentDecl")) {
                    cnode += 1;
                }
                else {
                    cnode += 1;
                    if (node.Labels.Contains("OperatorExpression")) {
                        string op = (string)node.Tags["operator"];
                        if (op == "<<") {
                            var rnode = node.Leaves[1];
                            if (rnode.Tags.ContainsKey("blank") && rnode.Leaves.Count == 1) {
                                var browse_node = rnode.Leaves[0];
                                var receiver = GetExpression(node.Leaves[0]);
                                if (browse_node.Tags.ContainsKey("bare") && browse_node.Tags["bare"] as string == "browse") {
                                    yield return new DMASTProcStatementBrowse(node.Location, receiver, GetExpression(browse_node.Leaves[0]), GetExpression(browse_node.Leaves[1]));
                                    continue;
                                }
                                else if (browse_node.Tags.ContainsKey("bare") && browse_node.Tags["bare"] as string == "browse_rsc") {
                                    if (browse_node.Leaves.Count == 1) {
                                        yield return new DMASTProcStatementBrowseResource(node.Location, receiver, GetExpression(browse_node.Leaves[0]), new DMASTConstantNull(node.Location));
                                    }
                                    else {
                                        yield return new DMASTProcStatementBrowseResource(node.Location, receiver, GetExpression(browse_node.Leaves[0]), GetExpression(browse_node.Leaves[1]));
                                    }
                                    continue;
                                }
                                else if (browse_node.Tags.ContainsKey("bare") && browse_node.Tags["bare"] as string == "output") {
                                    yield return new DMASTProcStatementOutputControl(node.Location, receiver, GetExpression(browse_node.Leaves[0]), GetExpression(browse_node.Leaves[1]));
                                    continue;
                                }
                            }
                        }
                    }
                    if (node.Labels.Contains("BuiltinExpression")) {
                        string proc_ident = (string)node.Tags["bare"];
                        if (proc_ident == "del") {
                            yield return new DMASTProcStatementDel(node.Location, GetExpression(node.Leaves[0]));
                            continue;
                        }
                    }
                    yield return new DMASTProcStatementExpression(node.Location, GetExpression(node));
                }
            }
        }

        Stack<Node> procVarPathStack = new();
        IEnumerable<DMASTProcStatementVarDeclaration> GetProcVarDefinitions(Node node) {
            if (node.Labels.Contains("PathTerminated")) {
                procVarPathStack.Push(node);
                DMASTExpression expr = new DMASTConstantNull(node.Location);
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
                        // TODO handle this
                    }
                    else if (modnode.Labels.Contains("InModifier")) {
                        // TODO handle this
                    }
                    else {
                        throw node.Error("Unknown object var declaration modifier");
                    }

                }
                var dpath = ExtractPath(procVarPathStack);
                dpath.Type = DreamPath.PathType.Relative;
                while (dpath.FindElement("var") == 0) {
                    dpath = dpath.RemoveElement(0);
                }
                var path = new DMASTPath(node.Location, new DreamPath($"var").Combine(dpath));
                if (index_modifier) {
                    if (dpath.FindElement("list") == -1) {
                        path = new DMASTPath(node.Location, new DreamPath($"var/list").Combine(dpath));
                    }
                    if (array_size != null) {
                        var paras = new DMASTCallParameter[1];
                        paras[0] = new DMASTCallParameter(node.Location, array_size);
                        expr = new DMASTNewPath(node.Location, new DMASTPath(node.Location, new OpenDreamShared.Dream.DreamPath("/list")) ,paras);
                    }
                }
                var define = new DMASTProcStatementVarDeclaration(node.Location, path, expr);
                yield return define;
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
            return (new DMASTProcStatementFor(nodes[0].Location, null, null, null, DMValueType.Anything, null), cnode);
        }

        /*
        (DMASTProcStatement, int) ForLoop(List<Node> nodes, int cnode, DMASTProcStatementVarDeclaration initializer = null, string decl_name = null) {
                if (cnode >= nodes.Count) {
                    return (null, 0);
                }
                var for_node = nodes[cnode];
                if (for_node.Labels.Contains("ForStmt")) {
                    cnode += 1;
                    if (for_node.Leaves.Count == 0) {
                        cnode += 1;
                        return (new DMASTProcStatementExpression(for_node.Location, new DMASTConstantString(for_node.Location, "This is a ForBody placeholder")), cnode);
                    }
                    if (for_node.Leaves.Count > 1) {
                        DMASTExpression init = null;
                        DMASTExpression compa = null;
                        DMASTExpression incr = null;
                        init = GetProcStatements(for_node.Leaves).ToList()[0];
                        compa = GetExpression(for_node.Leaves[1]);
                        if (for_node.Leaves.Count > 2) {
                            incr = GetExpression(for_node.Leaves[2]);
                        }
                        var for_body_node = for_node.Next();
                        DMASTProcBlockInner for_body = null;
                        init = FixInitializer(initializer, init);
                        if (for_body_node != null && for_body_node.Labels.Contains("ForBody")) {
                            cnode += 1;
                            for_body = GetProcBlockInner(for_body_node.Leaves);
                        }
                        return (new DMASTProcStatementFor(for_node.Location, init, compa, compa, DMValueType.Anything, for_body), cnode);
                        //return (new DMASTProcStatementForStandard(for_node.Location, init, compa, incr, for_body), cnode);
                    }
                    if (for_node.Leaves.Count == 1) {
                        var for_expr = for_node.Leaves[0].IgnoreBlank();
                        if (for_expr.CheckTag("operator", "in") || for_expr.CheckTag("operator", "=")) {
                            var for_mod = for_expr.Leaves[0].IgnoreBlank();
                            string[] for_name;
                            var dm_type = OpenDreamShared.Dream.Procs.DMValueType.Anything;
                            if (for_mod.CheckTag("operator", "as")) {
                                for_name = for_mod.Leaves[0].Tags["ident"] as string[];
                                dm_type = ConvertDMValueType(for_mod.Leaves[1]);
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
                                DMASTProcStatement for_init = initializer;
                                if (to_expr.CheckTag("operator", "to")) {
                                    var range_start = GetExpression(to_expr.Leaves[0]);
                                    var range_end = GetExpression(to_expr.Leaves[1]);
                                    DMASTExpression range_step = new DMASTConstantInteger(for_node.Location, 1);
                                    if (to_expr.Leaves.Count == 3) {
                                        range_step = GetExpression(to_expr.Leaves[2]);
                                    }
                                    if (for_expr.CheckTag("operator", "=")) {
                                        if (initializer == null) {
                                            for_init = new DMASTProcStatementExpression(for_node.Location, new DMASTAssign(for_node.Location, GetExpression(for_expr.Leaves[0]), range_start));
                                        }
                                        else {
                                            initializer.Value = range_start;
                                            for_init = initializer;
                                        }
                                    }
                                    var for_stmt = new DMASTProcStatementFor(for_node.Location, null, null, null, 
                                        )
                                    return (new DMASTProcStatementForRange(for_node.Location, for_init, ,
                                        range_start, range_end, range_step, body), cnode);
                                }
                                else {
                                    return (new DMASTProcStatementForList(for_node.Location, for_init, new DMASTIdentifier(for_node.Location, for_name[0]),
                                        GetExpression(for_expr.Leaves[1]), body), cnode);
                                }
                            }
                            var list_expr = GetExpression(for_expr.Leaves[1]);
                            return (new DMASTProcStatementForList(for_node.Location, initializer, new DMASTIdentifier(for_node.Location, for_name[0]), list_expr, body), cnode);
                        }
                        else if (for_expr.Tags.ContainsKey("ident")) {
                            var body_node = for_node.Next();
                            DMASTProcBlockInner body = null;
                            if (body_node != null && body_node.Labels.Contains("ForBody")) {
                                cnode += 1;
                                body = GetProcBlockInner(body_node.Leaves);
                            }
                            var loop_in_world = new DMASTProcStatementForList(for_node.Location, initializer,
                               new DMASTIdentifier(for_expr.Location, (for_expr.Tags["ident"] as string[])[0]), new DMASTIdentifier(for_node.Location, "world"), body);
                            return (loop_in_world, cnode);
                        }
                    }
                    throw for_node.Error("invalid for loop");
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
            */
        }
    }