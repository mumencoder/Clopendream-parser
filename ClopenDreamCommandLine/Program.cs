﻿using System;
using System.IO;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using DMCompiler.Compiler.DM;
using DMCompiler;
using System.Dynamic;
using Newtonsoft.Json;

namespace ClopenDream {
    class Program {

        static DirectoryInfo working_dir;
        static DMCompilerState open_compile;

        static dynamic json_output = new ExpandoObject();
        static int mismatch_count = 0;
        static List<string> mismatch_output = new();
        static int return_code = 255;

        static int Main(string[] args) {
            var rootCommand = new RootCommand();
            rootCommand.Description = "ClopenDream";

            DMCompiler.DMCompiler.Settings.SuppressUnimplementedWarnings = true;

            var command = new Command("parse") {
                new Argument<FileInfo>("byond_codetree", "Input code tree"),
            };
            command.Description = "Compile a DM file to OpenDream JSON";
            command.Handler = CommandHandler.Create<FileInfo>(Parse_Handler);
            rootCommand.AddCommand(command);

            command = new Command("object-hash") {
                new Argument<FileInfo>("byond_codetree", "Input code tree"),
                new Argument<FileInfo>("dm_original", "Original DM file"),
                new Option<DirectoryInfo>("--working_dir", getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()), "Directory containing empty.dm" ),
            };
            command.Description = "Write define hashes to file";
            command.Handler = CommandHandler.Create<FileInfo, FileInfo, DirectoryInfo>(Test_Object_Hash);
            rootCommand.AddCommand(command);

            command = new Command("compare") {
                new Argument<FileInfo>("byond_codetree", "Input code tree"),
                new Argument<FileInfo>("dm_original", "Original DM file"),
                new Option<DirectoryInfo>("--working_dir", getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()), "Directory containing empty.dm" ),
            };
            command.Description = "Compare AST for clopendream and opendream";
            command.Handler = CommandHandler.Create<FileInfo, FileInfo, DirectoryInfo>(Compare_Handler);
            rootCommand.AddCommand(command);

            rootCommand.Invoke(args);
            File.WriteAllText(Path.Combine(working_dir.FullName, "clopen_result.json"), JsonConvert.SerializeObject(json_output));

            return return_code;
        }

        static void Parse_Handler(FileInfo byond_codetree) {
            Program.working_dir = byond_codetree.Directory;
            if (ClopenParse(byond_codetree, null, out DMASTFile ast)) {
                return_code = 0;
                var astSrslr = new DMASTSerializer(ast);
                File.WriteAllText(Path.Combine(working_dir.FullName, "clopen_ast.json"), astSrslr.Result);
            } else {
                return_code = 1;
            }
        }

        static void Test_Object_Hash(FileInfo byond_codetree, FileInfo dm_original, DirectoryInfo working_dir) {
            Program.working_dir = working_dir;
            //DMCompiler.DMCompiler.Settings.ExperimentalPreproc = true;

            if (!ClopenParse(byond_codetree, null, out var clopen_ast)) {
                return_code = 1;
                return;
            }
            open_compile = DMCompiler.DMCompiler.GetAST(new() { dm_original.FullName });

            var clopen_hasher = new DMAST.ASTHasher();
            clopen_hasher.HashFile(clopen_ast);
            var f1 = File.CreateText(Path.Combine(working_dir.FullName, $"clopen-defs.txt"));
            foreach (var h in clopen_hasher.nodes.Keys) {
                f1.WriteLine(h);
            }
            f1.Close();

            var open_hasher = new DMAST.ASTHasher();
            open_hasher.HashFile(open_compile.ast);
            var f2 = File.CreateText(Path.Combine(working_dir.FullName, $"open-defs.txt"));
            foreach (var h in open_hasher.nodes.Keys) {
                f2.WriteLine(h);
            }
            f2.Close();

            return_code = 0;
        }
        static void Compare_Handler(FileInfo byond_codetree, FileInfo dm_original, DirectoryInfo working_dir) {
            try {
                Program.working_dir = working_dir;
                //DMCompiler.DMCompiler.Settings.ExperimentalPreproc = false;
                open_compile = DMCompiler.DMCompiler.GetAST(new() { dm_original.FullName });
                if (!ClopenParse(byond_codetree, Program.Compare, out var ast)) {
                    return_code = 1;
                    return;
                };
                return_code = 0;
            } catch (Exception e) {
                json_output.uncaught_exception = true;
                Console.WriteLine(e.ToString() );
                return_code = 1;
            }
        }
        static void Compare_Handler_ODFirst(FileInfo byond_codetree, FileInfo dm_original, DirectoryInfo working_dir) {
            Program.working_dir = working_dir;
            //DMCompiler.DMCompiler.Settings.ExperimentalPreproc = false;
            open_compile = DMCompiler.DMCompiler.GetAST(new() { dm_original.FullName });
            if (!ClopenParse(byond_codetree, null, out var clopen_AST)) {
                return_code = 1;
                return;
            }

            var openHash = new DMAST.ASTHasher();
            openHash.HashFile(open_compile.ast);

            var clopenHash = new DMAST.ASTHasher();
            clopenHash.HashFile(clopen_AST);

            var compare = new ASTComparer(clopenHash);
            foreach(var nodes in openHash.nodes.Values) {
                foreach(var node in nodes) {
                    compare.DefineComparer(node);
                }
            }
            return_code = 0;
        }

        static bool ClopenParse(FileInfo byond_codetree, Action<ConvertAST> handler, out DMASTFile ast_clopen) {
            Parser p = new();
            ast_clopen = null;
            Node root = null;
            try {
                root = p.BeginParse(byond_codetree.OpenText());
            } catch (ByondCompileError be) {
                json_output.byond_compile_error = be.Text;
                return false;
            }
            root.FixLabels();

            string filename = Path.Combine(working_dir.FullName, "empty.dm");
            DMCompilerState empty_compile = DMCompiler.DMCompiler.GetAST(new() { filename });
            FileInfo empty_code_tree = new FileInfo(Path.Combine(working_dir.FullName, "empty.out.txt"));
            Node empty_root = p.BeginParse(empty_code_tree.OpenText());
            empty_root.FixLabels();
            new FixEmpty(empty_root, root).Begin();

            var converter = new ConvertAST();
            if (handler != null) { handler(converter); }

            try {
                ast_clopen = converter.GetFile(root);
                ASTMerge.Merge(empty_compile.ast, ast_clopen);
            }
            catch {
                Console.WriteLine(converter.ProcNode.PrintLeaves(3));
                throw;
            }
            json_output.mismatch_count = mismatch_count;
            json_output.mismatch_output = mismatch_output;
            json_output.open_compile_errors = open_compile.parserErrors;
            json_output.empty_compile_errors = empty_compile.parserErrors;
            return true;
        }

        static void Compare(ConvertAST converter) {
            ASTComparer comparer;

            var open_hasher = new DMAST.ASTHasher();
            open_hasher.HashFile(open_compile.ast);
            comparer = new ASTComparer(open_hasher);
            converter.VisitDefine = DefineCompare;
            comparer.MismatchEvent = (x) => ProcessMismatchResult(x, converter);
            json_output.mismatch_count = 0;

            void DefineCompare(Node n, DMASTNode node) {
                comparer.DefineComparer(node);
            }
        }

        static void ProcessMismatchResult(List<ASTCompare> compares, ConvertAST converter) {
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

     }
}
