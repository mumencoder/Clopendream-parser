using System.Linq;
using OpenDreamShared.Dream;
using OpenDreamShared.Compiler.DM;

namespace ClopenDream {
    public class ASTMerge {
        public static void Merge(DMASTFile from, DMASTFile to) {
            to.BlockInner = new DMASTBlockInner(from.BlockInner.Statements.Concat(to.BlockInner.Statements).ToArray());
        }
    }
}