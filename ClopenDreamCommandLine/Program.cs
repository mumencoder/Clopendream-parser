using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DMPreprocessor;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using Newtonsoft.Json;

namespace ClopenDream {
    class Program {

        static int Main(string[] args) {
            var rootCommand = new RootCommand();
            rootCommand.Description = "ClopenDream";

            var command = new Command("parse") {
                new Argument<FileInfo>("byond_codetree", "Input code tree"),
                new Argument<FileInfo>("dm_original", "Original DM file"),
                new Argument<DirectoryInfo>("empty_dir", "Directory containing empty.dm"),
                new Argument<FileInfo>("json_file", "AST JSON output"),
                new Option<string>("--mode", getDefaultValue: () => "clopen")
            }; 
            command.Description = "Parse a DM file";
            command.Handler = CommandHandler.Create<FileInfo, FileInfo, DirectoryInfo, FileInfo, string>(ParseHandler);
            rootCommand.AddCommand(command);

            command = new Command("compare") {
                new Argument<FileInfo>("ast_l_file", "AST #1"),
                new Argument<FileInfo>("ast_r_file", "AST #2"),
                new Argument<DirectoryInfo>("output_dir", "Output directory")
            };
            command.Description = "Compare two AST files";
            command.Handler = CommandHandler.Create<FileInfo, FileInfo, DirectoryInfo>(CompareHandler);
            rootCommand.AddCommand(command);

            command = new Command("compare-tokens") {
                new Argument<FileInfo>("dm_file", "DM file"),
            };
            command.Description = "Compare opendream experimental token stream";
            command.Handler = CommandHandler.Create<FileInfo>(CompareTokensHandler);
            rootCommand.AddCommand(command);

            command = new Command("parse-compare") {
                new Argument<FileInfo>("byond_codetree", "Input code tree"),
                new Argument<FileInfo>("dm_file", "Original DM file"),
                new Argument<DirectoryInfo>("empty_dir", "Directory containing empty.dm"),
                new Argument<DirectoryInfo>("output_dir", "Output directory")
            };
            command.Description = "Parse a DM file with ClopenDream and compare it to an OpenDream AST file";
            command.Handler = CommandHandler.Create<FileInfo, FileInfo, DirectoryInfo, DirectoryInfo>(ParseCompareHandler);
            rootCommand.AddCommand(command);

            /*
            command = new Command("obj-tree") {
                new Argument<FileInfo>("dm_original", "Original DM file"),
                new Argument<DirectoryInfo>("output_dir", "Output directory"),
                new Option<string>("--mode", getDefaultValue: () => "original")
            };
            command.Description = "Create a DMObjectTree and write it to JSON";
            command.Handler = CommandHandler.Create<FileInfo, DirectoryInfo, string>(ObjTreeHandler);
            */

            command = new Command("obj-tree-compare") {
                new Argument<DirectoryInfo>("output_dir", "Output directory"),
            };
            command.Description = "Compare the object tree";
            command.Handler = CommandHandler.Create<DirectoryInfo>(ObjTreeCompareHandler);

            rootCommand.AddCommand(command);
            return rootCommand.InvokeAsync(args).Result;
        }

        static int ParseHandler(FileInfo byond_codetree, FileInfo dm_original, DirectoryInfo empty_dir, FileInfo json_file, string mode) {
            DMASTFile ast;
            if (mode == "clopen") {
                Console.WriteLine("clopen parse");
                ast = ClopenParse(byond_codetree, empty_dir);
            }
            else if (mode == "open") {
                Console.WriteLine("open parse");
                ast = OpenParse(dm_original);
            }
            else if (mode == "open-experimental") {
                Console.WriteLine("open-experimental parse");
                ast = DMCompiler.Program.ExperimentalCompile(dm_original.FullName);
            }
            else {
                throw new Exception("invalid moode " + mode);
            }
            var options = new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.All
            };
            File.WriteAllText(json_file.FullName, JsonConvert.SerializeObject(ast, options));
            return 0;
        }
        static int CompareHandler(FileInfo ast_l_file, FileInfo ast_r_file, DirectoryInfo output_dir) {
            DMASTFile ast_l = JsonConvert.DeserializeObject(File.ReadAllText(ast_l_file.FullName)) as DMASTFile;
            DMASTFile ast_r = JsonConvert.DeserializeObject(File.ReadAllText(ast_r_file.FullName)) as DMASTFile;
            CompareAST(output_dir, ast_l, ast_r);
            return 0;
        }

        static int CompareTokensHandler(FileInfo dm_file) {
            DMCompiler.Program.CompareTokens(new() { dm_file.FullName });
            return 0;
        }
        static int ParseCompareHandler(FileInfo byond_codetree, FileInfo dm_file, DirectoryInfo empty_dir, DirectoryInfo output_dir) {
            var open_ast = DMCompiler.Program.ExperimentalCompile(dm_file.FullName);
            ClopenParse(byond_codetree, empty_dir, open_ast, output_dir);
            return 0;
        }

        /*
        static int ObjTreeHandler(FileInfo dm_original, DirectoryInfo output_dir, string mode) {
            string json_file = null;
            if (mode == "original") {
                json_file = "original_objtree.json";
                DMCompiler.Program.CompileUsingOldParser(dm_original.FullName);
            }
            if (mode == "spaceman") {
                json_file = "spaceman_objtree.json";
                DMCompiler.Program.CompileUsingSpacemanParser(dm_original.FullName);
            }
            List<DreamPath> paths = new();
            foreach (var obj in DMCompiler.DM.DMObjectTree.AllObjects) {
                paths.Add(obj.Key);
            }
            File.WriteAllText(Path.Combine(output_dir.FullName, json_file), JsonConvert.SerializeObject(paths));
            return 0;
        }
        */

        static int ObjTreeCompareHandler(DirectoryInfo output_dir) {
            var paths1 = JsonConvert.DeserializeObject<List<DreamPath>>( File.ReadAllText(Path.Combine(output_dir.FullName, "original_objtree.json")));
            var paths2 = JsonConvert.DeserializeObject<List<DreamPath>>( File.ReadAllText(Path.Combine(output_dir.FullName, "spaceman_objtree.json")));

            HashSet<DreamPath> h1 = new(paths1);
            foreach (var path in paths2) {
                if (!h1.Contains(path)) {
                    Console.WriteLine("opendream is missing: " + path);
                }
            }
            return 0;
        }

        static void CompareAST(DirectoryInfo output_dir, DMASTFile ast_l, DMASTFile ast_r) {
            var hasher_l = new DMAST.ASTHasher();
            var hasher_r = new DMAST.ASTHasher();
            hasher_l.HashFile(ast_l);
            hasher_r.HashFile(ast_r);
            var comparer = new ASTComparer(hasher_l);
        }

        static DMASTFile OpenParse(FileInfo dm_original) {
            DMASTFile ast = GetAST(dm_original.FullName);
            return ast;
        }

        static DMASTFile ClopenParse(FileInfo byond_codetree, DirectoryInfo empty_dir, DMASTFile open_root = null, DirectoryInfo output_dir = null) {
            Parser p = new();
            Node root = p.BeginParse(byond_codetree.OpenText());
            root.FixLabels();

            DMASTFile ast_empty = GetAST(Path.Combine(empty_dir.FullName, "empty.dm"));
            FileInfo empty_code_tree = new FileInfo(Path.Combine(empty_dir.FullName, "empty-code-tree.txt"));
            Node empty_root = p.BeginParse(empty_code_tree.OpenText());
            empty_root.FixLabels();
            new FixEmpty(empty_root, root).Begin();

            int compared_ct = 0;
            var converter = new ConvertAST();
            ASTComparer comparer;
            if (open_root != null) {
                var hasher = new DMAST.ASTHasher();
                hasher.HashFile(open_root);
                comparer = new ASTComparer(hasher);
                converter.VisitDefine = DefineCompare;
                comparer.MismatchEvent = ProcessMismatchResult;
            }
            DMASTFile ast_clopen;
            try {
                ast_clopen = converter.GetFile(root);
                ASTMerge.Merge(ast_empty, ast_clopen);
            }
            catch {
                //Console.WriteLine(converter.ProcNode.PrintLeaves(20));
                throw;
            }
            return ast_clopen;

            void ProcessMismatchResult(DMASTNode clopen_node, List<DMASTNode> orig_nodes, List<ASTCompare.Result> results) {
                DMAST.DMASTNodePrinter printer = new();
                File.WriteAllText(Path.Combine(output_dir.FullName, "clopendream_ast.txt"), converter.clopen_to_closed_node[clopen_node].PrintLeaves(20));
                var f1 = File.CreateText(Path.Combine(output_dir.FullName, "error_clopen.txt"));
                printer.Print(clopen_node, f1, label: true);
                f1.Close();
                var f2 = File.CreateText(Path.Combine(output_dir.FullName, "error_open.txt"));
                foreach (var orig_node in orig_nodes) {
                    printer.Print(orig_node, f2, label: true);
                    f2.WriteLine("---------------");
                }
                f2.Close();
                foreach (var result in results) {
                    Console.WriteLine("Compared: " + compared_ct);
                    Console.WriteLine("=============");
                    Console.WriteLine(result.ResultType);
                    Console.WriteLine("-------------");
                    printer.Print(result.A, Console.Out);
                    Console.WriteLine();
                    Console.WriteLine("-------------");
                    printer.Print(result.B, Console.Out);
                    Console.WriteLine();
                }
                throw new Exception();
            }

            void DefineCompare(Node n, DMASTNode node) {
                compared_ct += 1;
                comparer.DefineComparer(node);
            }

        }

        static DMASTFile GetAST(string filename) {
            DMPreprocessor dmpp = DMCompiler.Program.Preprocess(new() { filename });
            DMLexer dmLexer = new DMLexer(null, dmpp.GetResult());
            DMParser dmParser = new DMParser(dmLexer);
            DMASTFile ast = dmParser.File();
            if (dmParser.Errors.Count > 0) {
                foreach (CompilerError error in dmParser.Errors) {
                    Console.WriteLine(error.ToString());
                }
            }
            return ast;
        }

     }
}
