
using DMCompiler.Compiler.DM;

namespace ClopenDream {
    class ASTModel {
        public DMASTFile FileNode;
        public DMAST.ASTHasher DefineHash;

        public ASTModel(DMASTFile file_node) { FileNode = file_node; }

        public void Update() {
            DefineHash = new DMAST.ASTHasher();
            DefineHash.HashFile(FileNode);
        }
    }

}