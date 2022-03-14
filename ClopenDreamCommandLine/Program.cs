﻿using System;
using System.IO;
using System.Reflection;
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
        static Dictionary<string, string> config;

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
                new Option<DirectoryInfo>("--working_dir", getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()), "Directory containing empty.dm" ),
            };
            command.Description = "Parse and serialize a codetree file";
            command.Handler = CommandHandler.Create<FileInfo, DirectoryInfo>(Parse_Handler);
            rootCommand.AddCommand(command);

            command = new Command("compare") {
                new Argument<FileInfo>("codetree_1", "Codetree #1"),
                new Argument<FileInfo>("codetree_2", "Codetree #2"),
                new Option<DirectoryInfo>("--working_dir", getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()), "Directory containing empty.dm" ),
            };
            command.Description = "Compare two serialized ASTs";
            command.Handler = CommandHandler.Create<FileInfo, FileInfo, DirectoryInfo>(Compare_Handler);
            rootCommand.AddCommand(command);

            command = new Command("object-hash") {
                new Argument<FileInfo>("byond_codetree", "Input code tree"),
                new Argument<FileInfo>("dm_original", "Original DM file"),
                new Option<DirectoryInfo>("--working_dir", getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()), "Directory containing empty.dm" ),
            };
            command.Description = "Write define hashes to file";
            command.Handler = CommandHandler.Create<FileInfo, FileInfo, DirectoryInfo>(Test_Object_Hash);
            rootCommand.AddCommand(command);

            var config_path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
            config = JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(config_path));
            rootCommand.Invoke(args);
            File.WriteAllText(Path.Combine(working_dir.FullName, "clopen_result.json"), JsonConvert.SerializeObject(json_output));

            return return_code;
        }

        static void Parse_Handler(FileInfo byond_codetree, DirectoryInfo working_dir) {
            Program.working_dir = working_dir;
            if (ClopenParse(byond_codetree, out DMASTFile ast)) {
                return_code = 0;
                var astSrslr = new DMASTSerializer(ast);
                File.WriteAllText(Path.Combine(byond_codetree.Directory.FullName, "clopen_ast.json"), astSrslr.Result);
            } else {
                return_code = 1;
            }
        }

        static void Compare_Handler(FileInfo codetree_1, FileInfo codetree_2, DirectoryInfo working_dir) {
            Program.working_dir = working_dir;
            try {
                JsonSerializerSettings settings = new() { TypeNameHandling = TypeNameHandling.All,   ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor };
                DMASTFile ast1 = JsonConvert.DeserializeObject<DMASTFile>(File.ReadAllText(codetree_1.FullName), settings);
                DMASTFile ast2 = JsonConvert.DeserializeObject<DMASTFile>(File.ReadAllText(codetree_2.FullName), settings);
                CompareAST(ast1, ast2);
                return_code = 0;
            } catch (Exception e) {
                json_output.uncaught_exception = true;
                Console.WriteLine(e.ToString());
                return_code = 1;
            }
        }

        static void Test_Object_Hash(FileInfo byond_codetree, FileInfo dm_original, DirectoryInfo working_dir) {
            Program.working_dir = working_dir;
            //DMCompiler.DMCompiler.Settings.ExperimentalPreproc = true;

            if (!ClopenParse(byond_codetree, out var clopen_ast)) {
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

        static bool ClopenParse(FileInfo byond_codetree, out DMASTFile ast_clopen) {
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

            string filename = Path.Combine(config["empty_dir"], "empty.dm");
            DMCompilerState empty_compile = DMCompiler.DMCompiler.GetAST(new() { filename });
            json_output.empty_compile_errors = empty_compile.parserErrors;
            FileInfo empty_code_tree = new FileInfo(Path.Combine(config["empty_dir"], "empty.codetree"));
            Node empty_root = p.BeginParse(empty_code_tree.OpenText());
            empty_root.FixLabels();
            new FixEmpty(empty_root, root).Begin();

            var converter = new ConvertAST();

            try {
                ast_clopen = converter.GetFile(root);
                ASTMerge.Merge(empty_compile.ast, ast_clopen);
            }
            catch {
                Console.WriteLine(converter.ProcNode.PrintLeaves(3));
                throw;
            }
            return true;
        }

        static void CompareAST(DMASTFile ast1, DMASTFile ast2) {
            ASTComparer comparer = new ASTComparer(ast1, ast2);
            comparer.MismatchEvent = (_) => ProcessMismatchResult(_);
            json_output.mismatch_count = mismatch_count;
            json_output.mismatch_output = mismatch_output;
        }

        static void ProcessMismatchResult(List<ASTCompare> compares) {
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
