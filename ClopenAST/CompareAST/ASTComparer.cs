
using System;
using System.Collections.Generic;
using DMCompiler;
using DMCompiler.Compiler.DM;
using DMCompiler.DM.Visitors;

namespace DMCompiler.DM.Visitors {
    public class DMDefineVisitor : DMASTVisitor {
        Action<DMASTNode> Handler;

        public DMDefineVisitor(Action<DMASTNode> handler) {
            Handler = handler;
        }

        public void VisitFile(DMASTFile dmFile) {
            dmFile.BlockInner.Visit(this);
        }

        public void VisitBlockInner(DMASTBlockInner blockInner) {
            foreach (DMASTStatement statement in blockInner.Statements) {
                statement.Visit(this);
            }
        }

        public void VisitObjectDefinition(DMASTObjectDefinition statement) {
            Handler(statement);
            statement.InnerBlock?.Visit(this);
        }

        public void VisitObjectVarDefinition(DMASTObjectVarDefinition objectVarDefinition) {
            Handler(objectVarDefinition);
        }

        public void VisitMultipleObjectVarDefinitions(DMASTMultipleObjectVarDefinitions multipleObjectVarDefinitions) {
            foreach (DMASTObjectVarDefinition varDefinition in multipleObjectVarDefinitions.VarDefinitions) {
                varDefinition.Visit(this);
            }
        }

        public void VisitObjectVarOverride(DMASTObjectVarOverride objectVarOverride) {
            Handler(objectVarOverride);
        }

        public void VisitProcDefinition(DMASTProcDefinition procDefinition) {
            Handler(procDefinition);
        }
    }
}

namespace ClopenDream {
    public class ASTComparer {
        public Action<List<ASTCompare>> MismatchEvent = (_) => { };

        DMASTFile Ast1;
        DMASTFile Ast2;

        DMAST.ASTHasher AstHash1;
        DMAST.ASTHasher AstHash2;

        public ASTComparer(DMASTNode ast1, DMASTNode ast2) {
            Ast1 = ast1 as DMASTFile;
            Ast2 = ast2 as DMASTFile;
            AstHash1 = new DMAST.ASTHasher();
            AstHash1.HashFile(Ast1);
            AstHash2 = new DMAST.ASTHasher();
            AstHash2.HashFile(Ast2);
        }

        public void CompareAll() {
            new DMDefineVisitor(DefineComparer).VisitFile(Ast1);
        }
        public void DefineComparer(DMASTNode node1) {
            var nodes2 = AstHash2.GetNode(node1);
            if (nodes2 == null) {
                Console.WriteLine($"{node1.Location}: missing {DMAST.ASTHasher.Hash(node1 as dynamic)}");
                return;
            }
            var found_match = false;

            List<ASTCompare> compares = new();
            foreach (var node2 in nodes2) {
                var compare = new ASTCompare(node1, node2);
                if (compare.Success) {
                    found_match = true;
                }
                compares.Add(compare);
            }
            if (found_match == false) {
                Console.WriteLine($"{node1.Location}: mismatch {DMAST.ASTHasher.Hash(node1 as dynamic)}");
                MismatchEvent(compares);
            }
        }
    }
}