
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

        public static Dictionary<string,Object> PrepareAST(TextReader codetree, Node empty_root) {
            Dictionary<string,Object> result = new();

            Parser p = new();
            Node root = null;
            try {
                root = p.BeginParse(codetree);
            } catch (Exception e) {
                result["parse_exc"] = e;
            }
            root.FixLabels();
            new FixEmpty(empty_root, root).Begin();

            result["parser"] = p;
            result["root_node"] = root;

            var converter = new ConvertAST();
            try {
                result["ast_clopen"] = converter.GetFile(root);
            } catch (Exception e) {
                result["convert_exc"] = e;
            }

            return result;
        }

        //                        output.WriteLine(converter.ProcNode.PrintLeaves(3));
        //            ASTMerge.Merge(empty_compile.ast, ast_clopen);

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