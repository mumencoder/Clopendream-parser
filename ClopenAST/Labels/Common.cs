
namespace ClopenDream {

    public partial class LabelContext {
        bool CheckCursed(Node node) {
            if (!node.Tags.ContainsKey("blank") || node.Leaves.Count != 1) { return false; }
            return true;
        }

        List<Node> NotActuallyCursed(Node node) {
            List<Node> stmts = null;
            if (node.Trunk.Tags.ContainsKey("blank") && node.Trunk.Leaves.Count == 1) { stmts = node.Trunk.Leaves; }
            else if (node.Tags.ContainsKey("blank")) { stmts = node.Leaves; }
            return stmts;
        }

        Node Uncurse(Node node, bool required = false) {
            bool cursed = CheckCursed(node);
            if (!cursed && required) { return null; }
            else if (cursed) { return node.Leaves[0]; }
            else { return node; }
        }
    }
}