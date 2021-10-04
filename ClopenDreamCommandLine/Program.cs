using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DMPreprocessor;
using OpenDreamShared.Compiler.DM;
using Newtonsoft.Json;

namespace ClopenDream {
    class Program {

        static int Main(string[] args) {
            var rootCommand = new RootCommand();
            rootCommand.Description = "ClopenDream";

            var command = new Command("parse") {
                new Argument<FileInfo>("byond_codetree", "Input code tree"),
                new Argument<FileInfo>("dm_original", "Original DM file"),
                new Argument<DirectoryInfo>("working_dir", "Working directory for output"),
                new Argument<FileInfo>("json_file", "AST JSON output"),
                new Option<string>("--mode", getDefaultValue: () => "clopen")
            }; 
            command.Description = "Parse a DM file";
            command.Handler = CommandHandler.Create<FileInfo, FileInfo, DirectoryInfo, FileInfo, string>(ClopenParseHandler);
            rootCommand.AddCommand(command);

            command = new Command("compare") {
                new Argument<FileInfo>("ast_r_file", "AST #1"),
                new Argument<FileInfo>("ast_l_file", "AST #2"),
            };
            command.Description = "Compare two AST files";
            command.Handler = CommandHandler.Create<FileInfo, FileInfo>(ClopenCompareHandler);
            rootCommand.AddCommand(command);

            return rootCommand.InvokeAsync(args).Result;
        }

        static int ClopenParseHandler(FileInfo byond_codetree, FileInfo dm_original, DirectoryInfo working_dir, FileInfo json_file, string mode) {
            if (mode == "clopen") {
                Console.WriteLine("clopen parse");
                DMASTFile ast = ClopenParse(byond_codetree, dm_original, working_dir);
                File.WriteAllText(json_file.FullName, JsonConvert.SerializeObject(ast));
            }
            else if (mode == "open") {
                Console.WriteLine("open parse");
                DMASTFile ast = OpenParse(dm_original);
                File.WriteAllText(json_file.FullName, JsonConvert.SerializeObject(ast));
            }
            else {
                throw new Exception("invalid mode");
            }
            return 0;
        }

        static int ClopenCompareHandler(FileInfo ast_r_file, FileInfo ast_l_file) {
            DMASTFile ast_r = JsonConvert.DeserializeObject(ast_r_file.FullName) as DMASTFile;
            DMASTFile ast_l = JsonConvert.DeserializeObject(ast_l_file.FullName) as DMASTFile;
            return 0;
        }

        static DMASTFile OpenParse(FileInfo dm_original) {
            DMASTFile ast = GetAST(dm_original.FullName);
            return ast;
        }
        static DMASTFile ClopenParse(FileInfo byond_codetree, FileInfo dm_original, DirectoryInfo working_dir) {
            Parser p = new();
            Node root = p.BeginParse(byond_codetree.OpenText(), dm_original);
            root.FixLabels();

            DMASTFile ast_empty = GetAST(Path.Combine(working_dir.FullName, "empty.dm"));
            FileInfo empty_code_tree = new FileInfo(Path.Combine(working_dir.FullName, "empty-code-tree.txt"));
            Node empty_root = p.BeginParse(empty_code_tree.OpenText());
            empty_root.FixLabels();

            new FixEmpty(empty_root, root).Begin();

            var converter = new ConvertAST();
            DMASTFile ast_clopen;
            try {
                ast_clopen = converter.GetFile(root);
                ASTMerge.Merge(ast_empty, ast_clopen);
            }
            catch {
                Console.WriteLine(converter.ProcNode.PrintLeaves(20));
                throw;
            }
            return ast_clopen;
        }

        class ASTModel {
            public DMASTFile FileNode;
            public DMAST.ASTHasher DefineHash;

            public ASTModel(DMASTFile file_node) { FileNode = file_node; }

            public void Update() {
                DefineHash = new DMAST.ASTHasher();
                DefineHash.HashFile(FileNode);
            }
        }

        static DMASTFile GetAST(string filename) {
            DMPreprocessor dmpp = DMCompiler.Program.Preprocess(new List<string> { filename });
            DMLexer dmLexer = new DMLexer(null, dmpp.GetResult());
            DMParser dmParser = new DMParser(dmLexer);
            if (dmParser.Errors.Count > 0) {
                foreach (CompilerError error in dmParser.Errors) {
                    Console.WriteLine(error.ToString());
                }
            }
            DMASTFile ast = dmParser.File();
            return ast;
        }

     }
}
