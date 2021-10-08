using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClopenDream {
    public partial class ConvertAST {
        DMASTExpression GetExpression(Node node) {
            while (node.IgnoreBlank() != node) {
                node = node.IgnoreBlank();
            }
            if (node.Labels.Contains("NumericLiteral")) {
                return ConvertNumericLiteral((string)node.Tags["numeric"]); 
            }
            if (node.Labels.Contains("StringLiteral")) {
                if (node.Leaves.Count == 0) {
                    return new DMASTConstantString(EscapeString((string)node.Tags["string"]));
                }
                else {
                    var paras = node.Leaves.Select((n) => GetExpression(n)).ToArray();
                    for (int i = 0; i < paras.Length; i++) {
                        if (paras[i] is DMASTConstantNull) {
                            paras[i] = null;
                        }
                    }
                    return new DMASTStringFormat(EscapeString(FormatText(node.Tags["string"] as string)), paras);
                }

            }
            if (node.Labels.Contains("ResourceLiteral")) {
                return new DMASTConstantResource(node.Tags["resource"] as string);
            }
            if (node.Labels.Contains("NullLiteral")) {
                return new DMASTConstantNull();

            }
            if (node.Labels.Contains("OperatorExpression")) {
                return GetOperator(node);
            }
            if (node.Labels.Contains("ListExpression")) {
                List<DMASTCallParameter> paras = new();
                foreach (var leaf in node.Leaves) {
                    paras.Add(GetCallParameter(leaf));
                }
                return new DMASTList(paras.ToArray());

            }
            if (node.Labels.Contains("SelfExpression")) {
                return new DMASTCallableSelf();
            }
            if (node.Labels.Contains("PathConstant")) {
                string[] path_elements = (string[])node.Tags["path"];

                return new DMASTConstantPath(new DMASTPath(ConvertPath(node)));
            }
            if (node.Labels.Contains("IdentExpression")) {
                return ConvertDeref(node);
            }
            if (node.Labels.Contains("NewExpression")) {
                if (node.Leaves.Count == 0) {
                    return new DMASTNewInferred(null);
                }
                var paras = GetCallParameters(node.Leaves.Skip(1).ToList());
                if (node.Leaves.Skip(1).ToList().Count == 0) { paras = null; }
                if (node.Leaves[0].Labels.Contains("EmptyExpression")) {
                    if (node.Leaves.Count == 1) {
                        return new DMASTNewInferred(new DMASTCallParameter[0]);
                    }
                    return new DMASTNewInferred(paras);
                }
                else if (node.Leaves[0].Labels.Contains("PathConstant")) {
                    var path = new DMASTPath(ConvertPath(node.Leaves[0]));
                    if (node.Leaves.Count == 1) {
                        return new DMASTNewPath(path, new DMASTCallParameter[0]);
                    }
                    return new DMASTNewPath(path, paras);
                }
                else if (node.Leaves[0].Labels.Contains("IdentExpression")) {
                    DMASTExpression expr = ConvertDeref(node.Leaves[0]);
                    if (expr is DMASTIdentifier idexpr) {
                        return new DMASTNewIdentifier(idexpr, paras);
                    }
                    else if (expr is DMASTDereference deref_expr) {
                        return new DMASTNewDereference(deref_expr, paras);
                    }
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
                    return new DMASTListIndex(GetExpression(node.Leaves[0]), GetExpression(node.Leaves[1]));
                }
                return new DMASTListIndex(IndexInnerToNode(node), GetExpression(node.Leaves[0]));
            }
            if (node.Labels.Contains("CallExpression")) {
                var paras = GetCallParameters(node.Leaves);
                var callable = CallInnerToNode(node);
                return new DMASTProcCall(callable, paras);
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
                return new DMASTCall(callParams.ToArray(), procParams.ToArray());
            }
            if (node.Labels.Contains("ArgListExpression")) {
                return new DMASTProcCall(new DMASTCallableProcIdentifier("arglist"), GetCallParameters(node.Leaves) );
            }
            if (node.Labels.Contains("PrePostExpression")) {
                if (node.Tags["dot"] as string == "post++") {
                    if (node.Leaves.Count == 0) {
                        return new DMASTPostIncrement(IndexInnerToNode(node));
                    }
                    return new DMASTPostIncrement(GetExpression(node.Leaves[0]));
                }
                if (node.Tags["dot"] as string == "post--") {
                    if (node.Leaves.Count == 0) {
                        return new DMASTPostDecrement(IndexInnerToNode(node));
                    }
                    return new DMASTPostDecrement(GetExpression(node.Leaves[0]));
                }
            }
            if (node.Labels.Contains("EmptyExpression")) {
                //return null;
                return new DMASTConstantNull();
            }
            if (node.Labels.Contains("BuiltinExpression")) {
                string proc_ident = (string)node.Tags["bare"];
                if (proc_ident == "istype") {
                    if (node.Leaves.Count == 1) {
                        return new DMASTImplicitIsType(GetExpression(node.Leaves[0]));
                    }
                    else if (node.Leaves.Count == 2) {
                        return new DMASTIsType(GetExpression(node.Leaves[0]), GetExpression(node.Leaves[1]));
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
                            var pexpr = pv_node.Leaves[0];
                            picks.Add(new DMASTPick.PickValue(null, GetExpression(pexpr)));
                        }
                        else {
                            throw node.Error("GetExpression.Pick");
                        }
                    }
                    return new DMASTPick(picks.ToArray());
                }
                if (proc_ident == "locate") {
                    if (node.Leaves.Count == 3) {
                        return new DMASTLocateCoordinates(GetExpression(node.Leaves[0]), GetExpression(node.Leaves[1]), GetExpression(node.Leaves[2]));
                    }
                    if (node.Leaves.Count == 2) {
                        return new DMASTLocate(NullifyNull(GetExpression(node.Leaves[0])), GetExpression(node.Leaves[1]));
                    }
                    if (node.Leaves.Count == 1) {
                        return new DMASTLocate(NullifyNull(GetExpression(node.Leaves[0])), null);
                    }
                }
                if (proc_ident == "initial") {
                    return new DMASTInitial(GetExpression(node.Leaves[0]));
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
                    return new DMASTInput(args.Reverse<DMASTCallParameter>().ToArray(), vt, list_expr);
                }
                if (proc_ident == "text") {
                    var s = node.Leaves[0].Tags["string"] as string;
                    if (node.Leaves.Count == 1) {
                        return new DMASTConstantString(s);
                    }
                    return new DMASTStringFormat(EscapeString(FormatText(s)), node.Leaves.Skip(1).Select((n) => GetExpression(n)).ToArray());
                }
                var paras = GetCallParameters( node.Leaves );
                return new DMASTProcCall(new DMASTCallableProcIdentifier(proc_ident), paras.ToArray());
            }
            throw node.Error("GetExpression");
        }

    }
}