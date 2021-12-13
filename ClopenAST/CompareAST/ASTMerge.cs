using System.Linq;
using DMCompiler.Compiler.DM;

namespace ClopenDream {
    public class ASTMerge {
        public static void Merge(DMASTFile from, DMASTFile to) {
            to.BlockInner = new DMASTBlockInner(new OpenDreamShared.Compiler.Location(), from.BlockInner.Statements.Concat(to.BlockInner.Statements).ToArray());
        }
    }
}