
using System;
using System.Collections.Generic;
using DMCompiler.Compiler.DM;
using DMCompiler.DM.Visitors;

namespace ClopenDream {
    public class ASTComparer {
        DMASTSimplifier simplify = new();

        DMAST.ASTHasher _openAstHash = new();
        public Action<List<ASTCompare>> MismatchEvent = (_) => { };

        public ASTComparer(DMAST.ASTHasher open_ast_hash) {
            _openAstHash = open_ast_hash;
        }

        public void CompareTopLevel(DMAST.ASTHasher l_hash, DMAST.ASTHasher r_hash) {
            var def_n = 0;
            var missing_n = 0;
            List<(DMASTNode, DMASTNode)> mismatches = new();

            foreach (var kv in l_hash.nodes) {
                def_n += 1;
                if (!r_hash.nodes.ContainsKey(kv.Key)) {
                    missing_n += 1;
                }
                else {
                    var lnl = l_hash.nodes[kv.Key];
                    var lnr = r_hash.nodes[kv.Key];
                    if (lnl.Count != lnr.Count) {
                        //mismatches.Add((lnl, lnr));
                    }
                    if (lnl.GetType() != lnr.GetType()) {
                    }
                }
            }
            def_n = 0;
            missing_n = 0;
            foreach (var kv in r_hash.nodes) {
                def_n += 1;
                if (!l_hash.nodes.ContainsKey(kv.Key)) {
                    missing_n += 1;
                }
            }
        }

        public void DefineComparer(DMASTNode node) {
            var orig_nodes = _openAstHash.GetNode(node);
            if (orig_nodes == null) {
                Console.WriteLine($"OpenDream missing {DMAST.ASTHasher.Hash(node as dynamic)}");
                return;
            }
            var found_match = false;

            List<ASTCompare> compares = new();
            simplify.SimplifyAST(node);
            foreach (var orig_node in orig_nodes) {
                simplify.SimplifyAST(orig_node);
                var compare = new ASTCompare(node, orig_node);
                if (compare.Success) {
                    found_match = true;
                }
                compares.Add(compare);
            }
            if (found_match == false) {
                MismatchEvent(compares);
            }
        }
    }
}