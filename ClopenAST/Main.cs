
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Reflection;
global using System.Dynamic;
global using System.Linq;
global using System.Text.RegularExpressions;
global using System.Text;
global using System.Text.Json.Serialization;

using DMCompiler;
using DMCompiler.Compiler.DM;

namespace ClopenDream {

    public partial class ClopenDream {

        public static ExpandoObject PrepareAST(TextWriter output, TextReader codetree, TextReader empty_code_tree, DMCompilerState empty_compile) {
            dynamic result = new ExpandoObject();

            Parser p = new();
            DMASTFile ast_clopen = null;
            Node root = p.BeginParse(codetree);
            root.FixLabels();

            Node empty_root = p.BeginParse(empty_code_tree);
            empty_root.FixLabels();
            new FixEmpty(empty_root, root).Begin();

            var converter = new ConvertAST();

            try {
                ast_clopen = converter.GetFile(root);
                ASTMerge.Merge(empty_compile.ast, ast_clopen);
            } catch {
                if (converter.ProcNode != null) {
                    output.WriteLine(converter.ProcNode.PrintLeaves(3));
                } else {
                    output.WriteLine("unknown error");
                }
            }
            result.ast_clopen = ast_clopen;
            return result;
        }

        public static void SearchAST(ConvertAST converter, string obj_path, string name) {
            converter.VisitDefine = DefineSearch;
            void DefineSearch(Node n, DMASTNode node) {
                if (node is DMASTProcDefinition pd) {
                    if (pd.ObjectPath.ToString().Contains(obj_path) && pd.Name == name) {
                        new DMAST.DMASTNodePrinter().Print(node, Console.Out);
                    }
                }
            }
        }

        // Deserialize Settings
        // JsonSerializerSettings settings = new() { TypeNameHandling = TypeNameHandling.All, MaxDepth = 1024, ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor };
    }
}