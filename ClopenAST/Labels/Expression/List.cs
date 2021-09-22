
namespace DMTreeParse {
    public partial class LabelContext {
        bool CheckListExpression(Node node) {
            if (!node.CheckTag("bare", "list")) { return false; }
            if (!ParseLeaves(CheckListElement, node)) { return false; }
            return true;
        }

        bool CheckNewlistExpression(Node node) {
            if (!node.CheckTag("bare", "newlist")) { return false; }
            if (!ParseLeaves(CheckListElement, node)) { return false; }
            return true;
        }
        bool CheckArgListExpression(Node node) {
            if (!node.CheckTag("bare", "arglist")) { return false; }
            if (node.Leaves.Count != 1) { Error(); }

            if (!ParseLeaves(CheckExpression, node)) { return false; }
            return true;
        }
        bool CheckListElement(Node node) {
            if (Parse(CheckListAssign, node)) { return true; }
            if (Parse(CheckExpression, node)) { return true; }
            return Error();
        }

        bool CheckListAssign(Node node) {
            if (!node.CheckTag("operator", "=")) { return false; }
            if (node.Leaves.Count != 2) { Error(); }
            if (!Parse(CheckExpression, node.Leaves[0])) { Error(); }
            if (!Parse(CheckExpression, node.Leaves[1])) { Error(); }
            return true;
        }
    }
}