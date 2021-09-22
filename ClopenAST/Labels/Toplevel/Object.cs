
namespace DMTreeParse {
    public partial class LabelContext {
        bool CheckObjectAssignStmt(Node node) {
            if (!node.CheckTag("operator", "=")) { return false; }
            // TODO fix curses before labeling.
            if (node.Leaves.Count != 2) { return Error(); }
            if (!Parse(CheckObjectAssignLValue, node.Leaves[0])) { return Error(); }
            if (!Parse(CheckObjectAssignRValue, node.Leaves[1])) { return Error(); }
            return true;
        }
        bool CheckObjectAssignLValue(Node node) {
            // TODO maybe inspect all the things that arrive here better
            if (node.Tags.ContainsKey("ident")) { return true; }
            return Error();
        }
        bool CheckObjectAssignRValue(Node node) {
            if (Parse(CheckExpression, node)) { return true; }
            return false;
        }
        public bool CheckParentDecl(Node node) {
            if (node.CheckTag("keyword", "..")) { return true; }
            return false;
        }
        bool CheckChildDecl(Node node) {
            if (node.CheckTag("dot", "child_type")) { return true; }
            return false;
        }
    }
}