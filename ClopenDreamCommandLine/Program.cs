using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DMPreprocessor;
using OpenDreamShared.Compiler.DM;
using DMCompiler.DM.Visitors;

namespace DMTreeParse {
    class Program {

        static DirectoryInfo arg_working_dir = null;
        static int Main(string[] args) {
            var rootCommand = new RootCommand {
                new Argument<FileInfo>("codetree", "Input code tree"),
                new Argument<FileInfo>("original", "Original DM file"),
                new Argument<DirectoryInfo>("working_dir", "Working directory for output")
            };
            rootCommand.Description = "Clopendream";
            rootCommand.Handler = CommandHandler.Create<FileInfo, FileInfo, DirectoryInfo>((codetree, original, working_dir) =>
            {
                arg_working_dir = working_dir;
                Parser p = new();

                FileInfo empty_dm_file = new FileInfo( Path.Combine(working_dir.FullName, "empty.dm") );
                FileInfo empty_code_tree = new FileInfo( Path.Combine(working_dir.FullName, "empty-code-tree.txt") );

                Node empty_root = p.BeginParse(empty_code_tree.OpenText(), original.Directory.FullName);
                empty_root.FixLabels();

                Node root = p.BeginParse(codetree.OpenText(), original.Directory.FullName);
                root.FixLabels();

                DMPreprocessor dmpp = DMCompiler.Program.Preprocess( new List<string> { original.FullName } );
                DMLexer dmLexer = new DMLexer(null, dmpp.GetResult());
                DMParser dmParser = new DMParser(dmLexer);
                ast_open = dmParser.File();

                if (dmParser.Errors.Count > 0) {
                    foreach (CompilerError error in dmParser.Errors) {
                        Console.WriteLine(error.ToString());
                    }
                }

                open_hash = new DMAST.ASTHasher();
                open_hash.HashFile(ast_open);
                DMPreprocessor dmpp2 = DMCompiler.Program.Preprocess(new List<string> { original.FullName });
                DMLexer dmLexer2 = new DMLexer(null, dmpp.GetResult());
                DMParser dmParser2 = new DMParser(dmLexer);
                DMASTFile ast_empty = dmParser.File();

                new FixEmpty(empty_root, root).Begin();

                var converter = new ConvertAST();
                converter.VisitDefine = DefineComparer;

                try {
                    DMASTFile ast_clopen = converter.GetFile(root);
                    ASTMerge.Merge(ast_empty, ast_clopen);
                }
                catch {
                    Console.WriteLine(converter.ProcNode.PrintLeaves(20));
                    throw;
                }


                return 0;
            });
            return rootCommand.InvokeAsync(args).Result;
        }
        static DMASTFile ast_open;
        static DMAST.ASTHasher open_hash;
        static DMAST.DMASTNodePrinter printer = new();
        static DMASTSimplifier simplify = new();

        static (object, object, string, object) lastCompare;
        static void CompareResult(object nl, object nr, string ty, object msg) {
            lastCompare = (nl, nr, ty, msg);
        }

        static void PrintCompareResult(object nl, object nr, string s) {
            Console.WriteLine("=============");
            Console.WriteLine(s);
            Console.WriteLine("-------------");
            printer.Print(nl, Console.Out);
            Console.WriteLine();
            Console.WriteLine("-------------");
            printer.Print(nr, Console.Out);
            Console.WriteLine();
        }

        static void DefineComparer(Node n, DMASTNode node) {
//            return;

            var orig_nodes = open_hash.GetNode(node);
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
                File.WriteAllText(Path.Combine(arg_working_dir.FullName, "clopendream_ast.txt"), n.PrintLeaves(20));
                var f1 = File.CreateText(Path.Combine(arg_working_dir.FullName, "error_clopen.txt"));
                printer.Print(node, f1, label:true);
                f1.Close();
                var f2 = File.CreateText(Path.Combine(arg_working_dir.FullName, "error_open.txt"));
                foreach (var orig_node in orig_nodes) {
                    printer.Print(orig_node, f2, label:true);
                    f2.WriteLine("---------------");
                }
                f2.Close();
                PrintCompareResult(lastCompare.Item1, lastCompare.Item2, lastCompare.Item3);
                throw new Exception();
            }

        }
    }
}
