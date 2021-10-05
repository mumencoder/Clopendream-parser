
using System;
using System.Collections.Generic;
using OpenDreamShared.Compiler.DM;
using DMCompiler.DM.Visitors;

namespace ClopenDream {
    public class ASTComparer {
        List<ASTCompare.Result> compareResults = new();
        DMASTSimplifier simplify = new();

        DMAST.ASTHasher _openAstHash = new();
        public Action<DMASTNode, List<DMASTNode>, List<ASTCompare.Result>> MismatchEvent = (_,_,_) => { };

        public ASTComparer(DMAST.ASTHasher open_ast_hash) {
            _openAstHash = open_ast_hash;
        }

        void CompareResult(ASTCompare.Result result) {
            compareResults.Add(result);
        }

        public void DefineComparer(DMASTNode node) {
            compareResults.Clear();
            var orig_nodes = _openAstHash.GetNode(node);
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
                MismatchEvent(node, orig_nodes, compareResults);
                throw new Exception();
            }
        }
    }
}