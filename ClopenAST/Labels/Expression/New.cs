
namespace ClopenDream {
    public partial class LabelContext {
        bool CheckNewExpression(Node node) {
            if (!node.CheckTag("bare", "new")) { return false; }
            if (node.Leaves.Count > 0) {
                if (!Parse(CheckExpression, node.Leaves[0])) { Error(); }
            }
            var leaves = node.Leaves.Skip(1).TakeWhile((node) => true).ToList();
            if (!ParseNodes(CheckExpression, leaves)) { Error();  }
            return true;
        }

    }
}