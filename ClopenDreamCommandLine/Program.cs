using System;
using System.IO;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using DMCompiler.Compiler.DM;

namespace ClopenDream {
    class Program {

        static DirectoryInfo working_dir;
        static DMASTFile open_AST;

        static int Main(string[] args) {
            var rootCommand = new RootCommand();
            rootCommand.Description = "ClopenDream";

            var command = new Command("compile") {
                new Argument<FileInfo>("byond_codetree", "Input code tree"),
                new Option<DirectoryInfo>("--working_dir", getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()), "Output directory" ),
                new Option<FileInfo>("--dm_original", "Original DM file")
            };
            command.Description = "Compile a DM file to OpenDream JSON";
            command.Handler = CommandHandler.Create<FileInfo, DirectoryInfo, FileInfo>(Compile_Handler);
            rootCommand.AddCommand(command);

            command = new Command("test-parse") {
                new Argument<FileInfo>("byond_codetree", "Input code tree"),
                new Option<DirectoryInfo>("--working_dir", getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()), "Directory containing empty.dm" ),
                new Option<FileInfo>("--dm_original", "Original DM file")
            };
            command.Description = "Parse a DM file";
            command.Handler = CommandHandler.Create<FileInfo, DirectoryInfo, FileInfo>(Test_Parse_Handler);
            rootCommand.AddCommand(command);

            command = new Command("compare") {
                new Argument<FileInfo>("byond_codetree", "Input code tree"),
                new Argument<FileInfo>("dm_original", "Original DM file"),
                new Option<DirectoryInfo>("--working_dir", getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()), "Directory containing empty.dm" ),
            };
            command.Description = "Compare AST for clopendream and opendream";
            command.Handler = CommandHandler.Create<FileInfo, FileInfo, DirectoryInfo>(Compare_Handler);
            rootCommand.AddCommand(command);

            return rootCommand.InvokeAsync(args).Result;
        }

        static int Compile_Handler(FileInfo byond_codetree, DirectoryInfo working_dir, FileInfo dm_original) {
            Program.working_dir = working_dir;
            DMCompiler.DMCompiler.Settings.SuppressUnimplementedWarnings = true;
            DMASTFile ast = ClopenParse(byond_codetree, null);
            return DMCompiler.DMCompiler.CompileAST(ast);
        }

        static int Test_Parse_Handler(FileInfo byond_codetree, DirectoryInfo working_dir, FileInfo dm_original) {
            Program.working_dir = working_dir;
            DMASTFile ast = ClopenParse(byond_codetree, null);
            return 0;
        }

        static int Compare_Handler(FileInfo byond_codetree, FileInfo dm_original, DirectoryInfo working_dir) {
            Program.working_dir = working_dir;
            open_AST = GetAST(dm_original.FullName);
            ClopenParse(byond_codetree, Program.Compare);
            return 0;
        }
        static DMASTFile ClopenParse(FileInfo byond_codetree, Action<ConvertAST> handler) {
            Parser p = new();
            Node root = p.BeginParse(byond_codetree.OpenText());
            root.FixLabels();

            DMASTFile ast_empty = GetAST(Path.Combine(working_dir.FullName, "empty.dm"));
            FileInfo empty_code_tree = new FileInfo(Path.Combine(working_dir.FullName, "empty.out.txt"));
            Node empty_root = p.BeginParse(empty_code_tree.OpenText());
            empty_root.FixLabels();
            new FixEmpty(empty_root, root).Begin();

            var converter = new ConvertAST();
            if (handler != null) { handler(converter); }

            DMASTFile ast_clopen;
            try {
                ast_clopen = converter.GetFile(root);
                ASTMerge.Merge(ast_empty, ast_clopen);
            }
            catch {
                Console.WriteLine(converter.ProcNode.PrintLeaves(3));
                throw;
            }
            return ast_clopen;
        }

        static int mismatch_count = 0;
        static void Compare(ConvertAST converter) {
            ASTComparer comparer;

            var open_hasher = new DMAST.ASTHasher();
            open_hasher.HashFile(open_AST);
            comparer = new ASTComparer(open_hasher);
            converter.VisitDefine = DefineCompare;
            comparer.MismatchEvent = ProcessMismatchResult;
            mismatch_count = 0;

            void DefineCompare(Node n, DMASTNode node) {
                comparer.DefineComparer(node);
            }
            void ProcessMismatchResult(List<ASTCompare> compares) {
                if (mismatch_count > 1000) {
                    throw new Exception();
                }
                mismatch_count++;

                int mismatch_id = 0;
                foreach (var compare in compares) {
                    mismatch_id += 1;
                    var path = "mismatch-" + DMAST.ASTHasher.Hash((dynamic)compare.nl) as string;
                    path = path.Replace("/", "@");
                    var dir_path = Path.Combine(working_dir.FullName, path);
                    Directory.CreateDirectory(dir_path);
                    DMAST.DMASTNodePrinter printer = new();
                    File.WriteAllText(Path.Combine(dir_path, $"{mismatch_id}-nodes_clopen.txt"), converter.clopen_to_closed_node[compare.nl].PrintLeaves(20));
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

        static void Search(ConvertAST converter, string obj_path, string name) {
            converter.VisitDefine = DefineSearch;
            void DefineSearch(Node n, DMASTNode node) {
                if (node is DMASTProcDefinition pd) {
                    if (pd.ObjectPath.ToString().Contains(obj_path) && pd.Name == name) {
                        new DMAST.DMASTNodePrinter().Print(node, Console.Out);
                    }
                }
            }
        }
        static DMASTFile GetAST(string filename) {
            return DMCompiler.DMCompiler.GetAST(new() { filename });
        }

     }
}
