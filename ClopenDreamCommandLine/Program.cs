using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using DMCompiler.Compiler.DM;
using DMCompiler;
using DMCompiler.DM.Visitors;
using Newtonsoft.Json;

namespace ClopenDream {
    class Program {

        static DMCompilerState open_compile;
        static Dictionary<string, string> input_config;

        static int return_code = 255;

        static int Main(string[] args) {
            var rootCommand = new RootCommand();
            rootCommand.Description = "ClopenDream";

            DMCompiler.DMCompiler.Settings.SuppressUnimplementedWarnings = true;

            /*
            var command = new Command("parse") {
                new Argument<FileInfo>("input_config_path", "Input config"),
                new Argument<FileInfo>("output_json_path", "Output json")
            };
            command.Description = "Parse and serialize a codetree file";
            command.Handler = CommandHandler.Create<FileInfo, FileInfo>(Parse_Handler);
            rootCommand.AddCommand(command);

            var command = new Command("compare") {
                new Argument<FileInfo>("input_config_path", "Input config"),
                new Argument<FileInfo>("output_json_path", "Output json")
            };
            command.Description = "Compare two serialized ASTs";
            command.Handler = CommandHandler.Create<FileInfo, FileInfo>(Compare_Handler);
            rootCommand.AddCommand(command);

            rootCommand.Invoke(args);
            */
            return return_code;
        }

        static void Startup(FileInfo input_config_path) {
            input_config = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(input_config_path.FullName));
        }

        /*
        static void Shutdown(FileInfo output_json_path) {
            File.WriteAllText(output_json_path.FullName, JsonConvert.SerializeObject(json_output));
        }

        /*
        static void Parse_Handler(FileInfo input_config_path, FileInfo output_json_path) {
            Startup(input_config_path);
            if (ClopenParse(input_config["byond_codetree_path"], out DMASTFile ast)) {
                return_code = 0;
                var astSrslr = new DMASTSerializer(ast);
                File.WriteAllText(input_config["ast_path"], astSrslr.Result);
            } else {
                return_code = 1;
            }
            Shutdown(output_json_path);
        }

        static void Compare_Handler(FileInfo input_config_path, FileInfo output_json_path) {
            Startup(input_config_path);
            try {
                JsonSerializerSettings settings = new() { TypeNameHandling = TypeNameHandling.All, MaxDepth = 1024, ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor };
                DMASTFile ast1 = JsonConvert.DeserializeObject<DMASTFile>(File.ReadAllText(input_config["ast_1"]), settings);
                DMASTFile ast2 = JsonConvert.DeserializeObject<DMASTFile>(File.ReadAllText(input_config["ast_2"]), settings);
                CompareAST(ast1, ast2);
                return_code = 0;
            } catch (Exception e) {
                json_output.uncaught_exception = true;
                Console.WriteLine(e.ToString());
                return_code = 1;
            }
            Shutdown(output_json_path);
        }

        static void Test_Object_Hash(FileInfo byond_codetree, FileInfo dm_original, DirectoryInfo working_dir) {
            if (!ClopenParse(byond_codetree.OpenText(), out var clopen_ast)) {
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

        static bool ClopenParse(TextReader codetree, out DMASTFile ast_clopen) {
            Parser p = new();
            ast_clopen = null;
            Node root = null;
            try {
                root = p.BeginParse(codetree);
            } catch (ByondCompileError be) {
                json_output.byond_compile_error = be.Text;
                return false;
            }
            root.FixLabels();

            string filename = Path.Combine(input_config["empty_dir"], "empty.dm");
            DMCompilerState empty_compile = DMCompiler.DMCompiler.GetAST(new() { filename });
            json_output.empty_compile_errors = empty_compile.parserErrors;
            FileInfo empty_code_tree = new FileInfo(Path.Combine(input_config["empty_dir"], "empty.codetree"));
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
        */

    }
}
