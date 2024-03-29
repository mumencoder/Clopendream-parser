﻿
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using DMCompiler.Compiler.DM;

namespace ClopenDream {
    public class ASTCompare {
        public List<Result> Results = new();
        public DMAST.Labeler Labeler = new();
        public DMASTNode nl, nr;
        public bool Success;

        public ASTCompare(DMASTNode _nl, DMASTNode _nr) {
            nl = _nl;
            nr = _nr;
            Success = Compare(nl, nr);
        }

        public static List<Type> equality_types = new() {
            typeof(char),
            typeof(string),
            typeof(int),
            typeof(float),
            typeof(bool),
            typeof(DMASTDereference.DereferenceType),
            typeof(DMValueType),
            typeof(DreamPath)
        };

        public static List<Type> ignore_types = new() {
            typeof(OpenDreamShared.Compiler.Location)
        };


        public class Result {
            public object A;
            public object B;
            public string ResultType;
            public object ResultValue;

            public Result(object a, object b, string result_type, object result_value = null) {
                A = a;
                B = b;
                ResultType = result_type;
                ResultValue = result_value;
            }
        }

        public Result CompareObjects(object node_l, object node_r) {
            Labeler.Add(node_l);
            Labeler.Add(node_r);
            var compare_ty = node_l.GetType();

            // TODO: temporary fix
            if (node_r is DMASTProcStatementInfLoop) {
                return null;
            }
            // TODO: temporary fix
            if (node_l is DMASTProcStatementExpression ast_pse && node_r is DMASTProcStatementVarDeclaration ast_psvd) {
                if (ast_pse.Expression is DMASTIdentifier id && id.Identifier == ast_psvd.Name) {
                    return null;
                }
                return new(node_l, node_r, $"node mismatch");
            }
            if (compare_ty.IsAssignableTo(typeof(DMASTNode)) || compare_ty.IsAssignableTo(typeof(DMASTCallable))) {
                // Note byond truncates .0 for constants
                if (node_l is DMASTConstantInteger || node_r is DMASTConstantFloat) {
                    return null;
                }
                // note byond converts a number like 1000000
                if (node_l is DMASTConstantFloat || node_r is DMASTConstantInteger) {
                    return null;
                }
                // note byond does not have a reasonable precedence for the in operator
                if (node_l is DMASTExpressionIn || node_r is DMASTExpressionIn) {
                    return null;
                }
            }
            // byond optimizes ternary expressions
            if (node_l is DMASTProcStatementIf if_node && node_r is DMASTProcStatementExpression stexpr && stexpr.Expression is DMASTTernary) {
                return null;
            }

            if (node_l.GetType() != node_r.GetType()) { return new(node_l, node_r, "type mismatch"); }

            if (ignore_types.Contains(node_l.GetType())) {
                return null;
            }
            if (node_l is float fl && node_r is float fr) {
                if (Math.Min(fl,fr) / Math.Max(fl,fr) < 0.99) {
                    return new(node_l, node_r, $"float mismatch {Math.Abs(fl - fr)}");
                }
                return null;
            }
            if (equality_types.Contains(compare_ty)) {
                if (!node_l.Equals(node_r)) {
                    return new(node_l, node_r, "equality mismatch", $"{node_l.GetType().FullName}");
                }
                return null;
            }

            if (compare_ty.IsAssignableTo(typeof(DMASTNode))) { return null; }
            if (compare_ty.IsAssignableTo(typeof(VarDeclInfo))) { return null; }
            if (compare_ty.IsAssignableTo(typeof(DMASTPick.PickValue))) { return null; }
            if (compare_ty.IsAssignableTo(typeof(DMASTProcStatementSwitch.SwitchCase))) { return null; }
            if (compare_ty.IsArray) { return null; }

            throw new Exception($"Unknown compare type {compare_ty.FullName}");
        }

        public bool Compare(object node_l, object node_r) {
            if (node_l == null || node_r == null) {
                if (node_r == node_l) { return true; }
                // note sometimes null, sometimes not in OD
                if (node_l is DMASTConstantNull || node_r is DMASTConstantNull) {
                    return true;
                }
                if (node_l is DMASTProcBlockInner ast_pbi) {
                    if (ast_pbi.Statements.Length == 0) {
                        return true;
                    }
                    return false;
                }
                if (node_l is DMASTProcStatementExpression node_pse) {
                    if (node_pse.Expression is DMASTConstantNull) {
                        return true;
                    }
                    Results.Add(new(node_l, node_r, "null mismatch"));
                    return false;
                }
                if (node_l is DMASTCallParameter[] || node_r is DMASTCallParameter[]) {
                    return true;
                }
                Results.Add(new(node_l, node_r, "null mismatch"));
                return false;
            }

            Result r = CompareObjects(node_l, node_r);
            if (r != null) {
                Results.Add(r);
                return false;
            };

            Type nty = Nullable.GetUnderlyingType(node_l.GetType());
            if (nty == null) {
                nty = node_l.GetType();
            }
            if (equality_types.Contains(nty)) {
                return true;
            }
            if (ignore_types.Contains(nty)) {
                return true;
            }

            if (nty.IsArray) {
                var al = (Array)node_l;
                var ar = (Array)node_r;
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
                    if (al == ar) { return true; }
                    Results.Add(new(node_l, node_r, "array mismatch", node_l.GetType().FullName));
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
                        var compare_node = new DMASTCallParameter(new OpenDreamShared.Compiler.Location(), new DMASTConstantNull(new OpenDreamShared.Compiler.Location()));
                        if (!Compare(extra, compare_node)) {
                            Results.Add(new(al, ar, "array length mismatch", extra));
                            return false;
                        }
                        null_ct++;
                    }
                }
                if (Math.Abs(lo.Length - hi.Length) != null_ct) {
                    Results.Add(new(node_l, node_r, "field mismatch array length", ""));
                    return false;
                }
                for (var i = 0; i < lo.Length; i++) {
                    if (!Compare(al.GetValue(i), ar.GetValue(i))) {
                        return false;
                    }
                }
                return true;
            }

            if (node_l.GetType() != node_r.GetType()) { return true; }

            foreach (var field in node_l.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)) {
                object vl = field.GetValue(node_l);
                object vr = field.GetValue(node_r);

                if (!Compare(vl, vr)) {
                    return false;
                }
            }

            return true;
        }

        static dynamic json_output = new ExpandoObject();

        static int mismatch_count = 0;
        static List<string> mismatch_output = new();

        static void CompareAST(DMASTFile ast1, DMASTFile ast2) {
            ASTComparer comparer = new ASTComparer(ast1, ast2);
            comparer.MismatchEvent = (_) => ProcessMismatchResult("", _);
            comparer.CompareAll();
            json_output.mismatch_count = mismatch_count;
            json_output.mismatch_output = mismatch_output;
        }

        static void ProcessMismatchResult(string mismatch_dir, List<ASTCompare> compares) {
            if (mismatch_count > 1000) {
                throw new Exception();
            }
            mismatch_count++;

            int mismatch_id = 0;
            foreach (var compare in compares) {
                mismatch_id += 1;
                var path = "mismatch-" + DMAST.ASTHasher.Hash((dynamic)compare.nl) as string;
                path = path.Replace("/", "@");
                mismatch_output.Add(path);
                var dir_path = Path.Combine(mismatch_dir, path);
                Directory.CreateDirectory(dir_path);
                DMAST.DMASTNodePrinter printer = new();
                var f1 = File.CreateText(Path.Combine(dir_path, $"{mismatch_id}-ast_clopen.txt"));
                printer.Print(compare.nl, f1, labeler: compare.Labeler);
                f1.Close();
                var f2 = File.CreateText(Path.Combine(dir_path, $"{mismatch_id}-ast_open.txt"));
                printer.Print(compare.nr, f2, labeler: compare.Labeler);
                f2.WriteLine("---------------");
                f2.Close();

                var f3 = File.CreateText(Path.Combine(dir_path, $"{mismatch_id}-compares.txt"));
                foreach (var result in compare.Results) {
                    f3.WriteLine("=============");
                    f3.WriteLine(result.ResultType);
                    f3.WriteLine("-------------" + result.A?.GetType());
                    printer.Print(result.A, f3, max_depth: 1, labeler: compare.Labeler);
                    f3.WriteLine();
                    f3.WriteLine("-------------" + result.B?.GetType());
                    printer.Print(result.B, f3, max_depth: 1, labeler: compare.Labeler);
                    f3.WriteLine();
                }
                f3.Close();
            }
        }
    }
}