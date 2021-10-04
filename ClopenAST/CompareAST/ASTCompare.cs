
using System;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Compiler.DM;

namespace ClopenDream {
    public class ASTCompare {
        public void CompareTopLevel(DMAST.ASTHasher l_hash, DMAST.ASTHasher r_hash) {
            var def_n = 0;
            var missing_n = 0;
            List<(DMASTNode, DMASTNode)> mismatches = new();
            
            foreach(var kv in l_hash.nodes) {
                def_n += 1;
                if (!r_hash.nodes.ContainsKey(kv.Key)) {
                    missing_n += 1;
                }
                else {
                    var lnl = l_hash.nodes[kv.Key];
                    var lnr = r_hash.nodes[kv.Key];
                    if (lnl.Count != lnr.Count) {
                        //mismatches.Add((lnl, lnr));
                    }
                    if (lnl.GetType() != lnr.GetType()) {
                    }
                }
            }
            def_n = 0;
            missing_n = 0;
            foreach (var kv in r_hash.nodes) {
                def_n += 1;
                if (!l_hash.nodes.ContainsKey(kv.Key)) {
                    missing_n += 1;
                }
            }
        }

        public static List<Type> equality_field_types = new() {
            typeof(string),
            typeof(int),
            typeof(float),
            typeof(bool),
            typeof(DMValueType)
        };

        public static bool Compare(object node_l, object node_r, Action<object, object, string, object> cr) {
            if (node_l == null || node_r == null) {
                if (node_r == node_l) { return true; }
                // note sometimes null, sometimes not in OD
                if (node_l is DMASTCallParameter[] || node_r is DMASTCallParameter[]) {
                    return true;
                }
                cr(node_l, node_r, "null mismatch", "");
                return false;
            }

            // byond optimizes ternary expressions
            if (node_l is DMASTProcStatementIf if_node && node_r is DMASTProcStatementExpression stexpr) {
                Console.WriteLine("!");
                if (if_node.Condition is DMASTNot && stexpr.Expression is DMASTTernary t) {
                    Console.WriteLine("!2");
                    if (t.B is DMASTConstantNull) {
                        Console.WriteLine("!3");
                        return true;
                    }
                }
            }
            if (node_l.GetType() != node_r.GetType()) { cr(node_l, node_r, "type mismatch", ""); return false; }

            foreach (var field in node_l.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)) {
                Type compare_ty = Nullable.GetUnderlyingType(field.FieldType);
                if (compare_ty == null) {
                    compare_ty = field.FieldType;
                }
                object vl = field.GetValue(node_l);
                object vr = field.GetValue(node_r);
                if (vl == null || vr == null) {
                    if (vl == vr) { continue; }
                    if ((vl is DMASTCallParameter[] && vr == null)) {
                        continue;
                    }
                    if ((vr is DMASTCallParameter[] && vl == null)) {
                        continue;
                    }
                    cr(vl, vr, "field mismatch", node_l.GetType().FullName + " . (" + field.FieldType.FullName + ") " + field.Name);
                    return false;
                }

                if (compare_ty.IsAssignableTo(typeof(DMASTNode))) {
                    // Note byond truncates .0 for constants
                    if (vl is DMASTConstantInteger || vr is DMASTConstantFloat) {
                        continue;
                    }
                    // note byond converts a number like 1000000
                    if (vl is DMASTConstantFloat || vr is DMASTConstantInteger) {
                        continue;
                    }
                    // note byond does not have a reasonable precedence for the in operator
                    if (vl is DMASTExpressionIn || vr is DMASTExpressionIn) {
                        continue;
                    }
                    if (!Compare(vl, vr, cr)) {
                        return false;
                    }
                }
                else if (compare_ty.IsArray) {
                    if ((vl is DMASTCallParameter[] && vr == null)) {
                        continue;
                    }
                    var al = (Array)vl;
                    var ar = (Array)vr;
                    // NOTE DMASTMultipleObjectVarDefinitions is an OD thing
                    List<object> new_r = new();
                    foreach (var rnode in ar) {
                        if (rnode is DMASTProcStatementMultipleVarDeclarations multi) {
                            new_r.AddRange(multi.VarDeclarations);
                        }
                        else {
                            new_r.Add(rnode);
                        }
                    }
                    ar = new_r.ToArray();
                    if (al == null || ar == null) {
                        if (al == ar) { continue; }
                        cr(al, ar, "field mismatch", node_l.GetType().FullName + " . (" + field.FieldType.FullName + ") " + field.Name);
                        return false;
                    }

                    int null_ct = 0;
                    Array lo, hi;
                    if (ar.Length > al.Length) {
                        hi = ar;
                        lo = al;
                    }
                    else {
                        hi = al;
                        lo = ar;
                    }

                    // NOTE byond drops an extra , which creates an implied null parameter in opendream
                    if (lo.Length != hi.Length) {
                        for (int i = lo.Length; i < hi.Length; i++) {
                            var extra = hi.GetValue(i) as DMASTNode;
                            if (!Compare(extra, new DMASTCallParameter(new DMASTConstantNull()), cr)) {
                                cr(al, ar, "array length mistmatch", extra);
                                return false;
                            }
                            null_ct++;
                        }
                    }
                    if (Math.Abs(lo.Length - hi.Length) != null_ct) {
                        cr(vl, vr, "field mismatch array length", "");
                        return false;
                    }
                    for (var i = 0; i < lo.Length; i++) {
                        if (!Compare(al.GetValue(i), ar.GetValue(i), cr)) {
                            return false;
                        }
                    }
                }
                else if (compare_ty.IsValueType) {
                    if (vl is int il && vr is int ir) {
                        if (il != ir) {
                            Console.WriteLine("int mismatch " + vl + " " + vr);
                            continue;
                        }
                    }
                    // note byond does not print floats with enough precision
                    if (vl is float fl && vr is float fr) {
                        if (Math.Abs(fl - fr) > (1 / 1024)) {
                            Console.WriteLine("float mismatch " + vl + " " + vr);
                            continue;
                        }
                    }
                    if ((vl == null && vr is DMASTCallParameter[])) {
                        continue;
                    }
                    if (!vl.Equals(vr)) {
                        cr(vl, vr, "field mismatch", node_l.GetType().FullName + " . (" + field.FieldType.FullName + ") " + field.Name);
                        return false;
                    }
                }
                else if (compare_ty.IsAssignableTo(typeof(DreamPath))) {
                    var pathr = vl as DreamPath?;
                    var pathl = vr as DreamPath?;
                    if (!pathr.Equals(pathl)) {
                        cr(vl, vr, "field mismatch", node_l.GetType().FullName + " . (" + field.FieldType.FullName + ") " + field.Name);
                        return false;
                    }
                }
                else if (equality_field_types.Contains(compare_ty)) {
                    if (!vl.Equals(vr)) {
                        cr(vl, vr, "field mismatch", node_l.GetType().FullName + " . (" + field.FieldType.FullName + ") " + field.Name);
                        return false;
                    }
                }
                else {
                    throw new Exception(node_l.GetType().FullName + " . (" + field.FieldType.FullName + ") " + field.Name);
                }
            }

            return true;
        }
    }
}