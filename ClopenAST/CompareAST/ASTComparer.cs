
using System;
using OpenDreamShared.Compiler.DM;
using DMCompiler.DM.Visitors;

namespace ClopenDream {
    class ASTComparer {
        (object, object, string, object) lastCompare;
        DMAST.DMASTNodePrinter printer = new();
        DMAST.ASTHasher open_ast_hash = new();
        DMASTSimplifier simplify = new();

        void CompareResult(object nl, object nr, string ty, object msg) {
            lastCompare = (nl, nr, ty, msg);
        }

        void PrintCompareResult(object nl, object nr, string s) {
            Console.WriteLine("=============");
            Console.WriteLine(s);
            Console.WriteLine("-------------");
            printer.Print(nl, Console.Out);
            Console.WriteLine();
            Console.WriteLine("-------------");
            printer.Print(nr, Console.Out);
            Console.WriteLine();
        }

        void DefineComparer(Node n, DMASTNode node) {
            var orig_nodes = open_ast_hash.GetNode(node);
            var found_match = false;

            if (node is DMASTProcDefinition pd) {
                // TODO OD doesnt handle an empty for body correctly
                if (pd.ObjectPath.ToString() == "/obj/machinery/computer/cloning" && pd.Name == "findscanner") {
                    return;
                }
                // NOTE byond drops the user param of this function
                if (pd.ObjectPath.ToString() == "/obj/structure/closet" && pd.Name == "toggle") {
                    return;
                }
            }
            foreach (var orig_node in orig_nodes) {
                simplify.SimplifyAST(node);
                simplify.SimplifyAST(orig_node);
                if (ASTCompare.Compare(node, orig_node, CompareResult)) {
                    found_match = true;
                }
            }
            if (found_match == false) {
                //File.WriteAllText(Path.Combine(arg_working_dir.FullName, "clopendream_ast.txt"), n.PrintLeaves(20));
                //var f1 = File.CreateText(Path.Combine(arg_working_dir.FullName, "error_clopen.txt"));
                //printer.Print(node, f1, label: true);
                //f1.Close();
                //var f2 = File.CreateText(Path.Combine(arg_working_dir.FullName, "error_open.txt"));
                //foreach (var orig_node in orig_nodes) {
                //    printer.Print(orig_node, f2, label: true);
                //    f2.WriteLine("---------------");
                //}
                //f2.Close();
                //PrintCompareResult(lastCompare.Item1, lastCompare.Item2, lastCompare.Item3);
                throw new Exception();
            }
        }
    }
}