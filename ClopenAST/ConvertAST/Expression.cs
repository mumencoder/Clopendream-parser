using OpenDreamShared.Dream.Procs;
using DMCompiler.Compiler.DM;

namespace ClopenDream {
    public partial class ConvertAST {
        DMASTExpression GetExpression(Node node) {
            while (node.IgnoreBlank() != node) {
                node = node.IgnoreBlank();
            }
            if (node.Labels.Contains("NumericLiteral")) {
                return ConvertNumericLiteral(node, (string)node.Tags["numeric"]); 
            }
            if (node.Labels.Contains("StringLiteral")) {
                string s = node.Tags["string"] as string;
                int format_args = CountFormatArgs(s);
                s = EscapeString(FormatText(s));
                
                if (format_args == 0) {
                    return new DMASTConstantString(node.Location, s);
                }
                else {
                    var paras = node.Leaves.Select((n) => GetExpression(n)).ToList();
                    for (int i = 0; i < paras.Count; i++) {
                        if (paras[i] is DMASTConstantNull) {
                            paras[i] = null;
                        }
                    }
                    while (paras.Count < format_args) {
                        paras.Add(null);
                    }
                    return new DMASTStringFormat(node.Location, s, paras.ToArray());
                }

            }
            if (node.Labels.Contains("ResourceLiteral")) {
                return new DMASTConstantResource(node.Location, node.Tags["resource"] as string);
            }
            if (node.Labels.Contains("NullLiteral")) {
                return new DMASTConstantNull(node.Location);

            }
            if (node.Labels.Contains("OperatorExpression")) {
                return GetOperator(node);
            }
            if (node.Labels.Contains("ListExpression")) {
                List<DMASTCallParameter> paras = new();
                foreach (var leaf in node.Leaves) {
                    paras.Add(GetCallParameter(leaf));
                }
                return new DMASTList(node.Location, paras.ToArray());

            }
            if (node.Labels.Contains("NewlistExpression")) {
                List<DMASTCallParameter> paras = new();
                foreach (var leaf in node.Leaves) {
                    paras.Add(GetCallParameter(leaf));
                }
                return new DMASTNewList(node.Location, paras.ToArray());
            }
            if (node.Labels.Contains("SelfExpression")) {
                return new DMASTCallableSelf(node.Location);
            }
            if (node.Labels.Contains("LotsaDots")) {
                string s = node.Tags["special"] as string;
                return new DMASTConstantPath(node.Location, new DMASTPath(node.Location, new OpenDreamShared.Dream.DreamPath(s)));
            }
            if (node.Labels.Contains("PathConstant")) {
                if (node.Leaves.Count > 0) {
                    Console.WriteLine($"{node.Location}: warning: modified types");
                }
                return ConvertPath(node);
            }
            if (node.Labels.Contains("IdentExpression")) {
                return ConvertDeref(node);
            }
            if (node.Labels.Contains("NewExpression")) {
                if (node.Leaves.Count == 0) {
                    return new DMASTNewInferred(node.Location, null);
                }
                var paras = GetCallParameters(node.Leaves.Skip(1).ToList());
                if (node.Leaves.Skip(1).ToList().Count == 0) { paras = null; }
                if (node.Leaves[0].Labels.Contains("EmptyExpression")) {
                    if (node.Leaves.Count == 1) {
                        return new DMASTNewInferred(node.Location, new DMASTCallParameter[0]);
                    }
                    return new DMASTNewInferred(node.Location, paras);
                }
                else if (node.Leaves[0].Labels.Contains("PathConstant")) {
                    if (node.Leaves[0].Leaves.Count > 0) {
                        Console.WriteLine($"{node.Location}: warning: modified types");
                    }
                    var path = new DMASTPath(node.Location, FlattenPath(node.Leaves[0]));
                    if (node.Leaves.Count == 1) {
                        return new DMASTNewPath(node.Location, path, new DMASTCallParameter[0]);
                    }
                    return new DMASTNewPath(node.Location, path, paras);
                }
                else if (node.Leaves[0].Labels.Contains("IdentExpression")) {
                    DMASTExpression expr = ConvertDeref(node.Leaves[0]);
                    if (expr is DMASTIdentifier idexpr) {
                        return new DMASTNewIdentifier(node.Location, idexpr, paras);
                    }
                    else if (expr is DMASTDereference deref_expr) {
                        return new DMASTNewDereference(node.Location, deref_expr, paras);
                    }
                }
                else if (node.Leaves[0].Labels.Contains("SelfExpression")) {
                    return new DMASTNewIdentifier(node.Location, new DMASTIdentifier(node.Location, "."), paras);
                }
                else {
                    throw node.Error("GetExpression.NewExpression");
                }
            }
            if (node.Labels.Contains("IndexExpression")) {
                if (node.Leaves.Count > 2) {
                    throw node.Error("IndexExpression");
                }
                if (node.Leaves.Count == 2) {
                    return new DMASTListIndex(node.Location, GetExpression(node.Leaves[0]), GetExpression(node.Leaves[1]), false);
                }
                return new DMASTListIndex(node.Location, IndexInnerToNode(node), GetExpression(node.Leaves[0]), false);
            }
            if (node.Labels.Contains("CallExpression")) {
                var paras = GetCallParameters(node.Leaves);
                var callable = CallInnerToNode(node);
                return new DMASTProcCall(node.Location, callable, paras);
            }
            if (node.Labels.Contains("DynamicCallExpression")) {
                List<DMASTCallParameter> callParams = new();
                List<DMASTCallParameter> procParams = new();
                if (node.Leaves.Count >= 1) {
                    if (node.Leaves[0].Tags.ContainsKey("blank")) {
                        callParams.AddRange( GetCallParameters(node.Leaves[0].Leaves));
                    }
                    else {
                        callParams.Add( GetCallParameter(node.Leaves[0]) );
                    }
                }
                if (node.Leaves.Count >= 2) {
                    if (node.Leaves[1].Tags.ContainsKey("blank")) {
                        procParams.AddRange(GetCallParameters(node.Leaves[1].Leaves));
                    }
                    else {
                        procParams.Add(GetCallParameter(node.Leaves[1]));
                    }
                }
                if (node.Leaves.Count >= 3) {
                    procParams.AddRange(GetCallParameters(node.Leaves.Skip(2).ToList()));
                }
                return new DMASTCall(node.Location, callParams.ToArray(), procParams.ToArray());
            }
            if (node.Labels.Contains("ArgListExpression")) {
                return new DMASTProcCall(node.Location, new DMASTCallableProcIdentifier(node.Location, "arglist"), GetCallParameters(node.Leaves) );
            }
            if (node.Labels.Contains("PrePostExpression")) {
                if (node.Tags["dot"] as string == "post++") {
                    if (node.Leaves.Count == 0) {
                        return new DMASTPostIncrement(node.Location, IndexInnerToNode(node));
                    }
                    return new DMASTPostIncrement(node.Location, GetExpression(node.Leaves[0]));
                }
                if (node.Tags["dot"] as string == "post--") {
                    if (node.Leaves.Count == 0) {
                        return new DMASTPostDecrement(node.Location, IndexInnerToNode(node));
                    }
                    return new DMASTPostDecrement(node.Location, GetExpression(node.Leaves[0]));
                }
            }
            if (node.Labels.Contains("EmptyExpression")) {
                //return null;
                return new DMASTConstantNull(node.Location);
            }
            if (node.Labels.Contains("BuiltinExpression")) {
                if (node.Tags.ContainsKey("special")) {
                    Console.WriteLine("warning: global vars");
                    return new DMASTConstantNull(node.Location);
                }
                string proc_ident = (string)node.Tags["bare"];
                if (proc_ident == "istype") {
                    if (node.Leaves.Count == 1) {
                        return new DMASTImplicitIsType(node.Location, GetExpression(node.Leaves[0]));
                    }
                    else if (node.Leaves.Count == 2) {
                        return new DMASTIsType(node.Location, GetExpression(node.Leaves[0]), GetExpression(node.Leaves[1]));
                    }
                    else {
                        throw node.Error("GetExpression.IsType");
                    }
                }
                if (proc_ident == "pick") {
                    List<DMASTPick.PickValue> picks = new();
                    foreach (var pv_node in node.Leaves) {
                        if (pv_node.Leaves.Count == 0) {
                            picks.Add(new DMASTPick.PickValue(null, GetExpression(pv_node)));
                            continue;
                        }
                        else if (pv_node.Tags.ContainsKey("blank")) {
                            if (pv_node.Leaves[0].CheckTag("bare", "prob")) {
                                var prob_node = pv_node.Leaves[0];
                                picks.Add(new DMASTPick.PickValue(GetExpression(prob_node.Leaves[0]), GetExpression(prob_node.Leaves[1])));
                            }
                            else {
                                var pexpr = pv_node.Leaves[0];
                                picks.Add(new DMASTPick.PickValue(null, GetExpression(pexpr)));
                            }
                        }
                        else {
                            throw node.Error("GetExpression.Pick");
                        }
                    }
                    return new DMASTPick(node.Location, picks.ToArray());
                }
                if (proc_ident == "locate") {
                    if (node.Leaves.Count == 3) {
                        return new DMASTLocateCoordinates(node.Location, GetExpression(node.Leaves[0]), GetExpression(node.Leaves[1]), GetExpression(node.Leaves[2]));
                    }
                    if (node.Leaves.Count == 2) {
                        return new DMASTLocate(node.Location, NullifyNull(GetExpression(node.Leaves[0])), GetExpression(node.Leaves[1]));
                    }
                    if (node.Leaves.Count == 1) {
                        return new DMASTLocate(node.Location, NullifyNull(GetExpression(node.Leaves[0])), null);
                    }
                    if (node.Leaves.Count == 0) {
                        return new DMASTLocate(node.Location, null, null);
                    }
                }
                if (proc_ident == "initial") {
                    return new DMASTInitial(node.Location, GetExpression(node.Leaves[0]));
                }
                if (proc_ident == "input") {
                    var vt = DMValueType.Text;
                    DMASTExpression list_expr = null;
                    List<DMASTCallParameter> args = new();
                    var remove_empty = true;
                    foreach (var para in node.Leaves.Take(4).Reverse()) {
                        var p = GetCallParameter(para);
                        if (remove_empty && p.Value == null) {
                            continue;
                        }
                        args.Add(p);
                        remove_empty = false;
                    }
                    if (node.Leaves.Count > 4) {
                        if (node.Leaves[4].Tags.ContainsKey("numeric")) {
                            vt = ConvertDMValueType(node.Leaves[4]);
                        }
                    }
                    if (node.Leaves.Count > 5) {
                        list_expr = GetExpression(node.Leaves[5]);
                    }
                    return new DMASTInput(node.Location, args.Reverse<DMASTCallParameter>().ToArray(), vt, list_expr);
                }
                if (proc_ident == "text") {
                    var s = node.Leaves[0].Tags["string"] as string;
                    if (node.Leaves.Count == 1) {
                        return new DMASTConstantString(node.Location, EscapeString(s));
                    }
                    return new DMASTStringFormat(node.Location, EscapeString(FormatText(s)), node.Leaves.Skip(1).Select((n) => GetExpression(n)).ToArray());
                }
                var paras = GetCallParameters( node.Leaves );
                return new DMASTProcCall(node.Location, new DMASTCallableProcIdentifier(node.Location, proc_ident), paras.ToArray());
            }
            throw node.Error("GetExpression");
        }

    }
}