
namespace DMTreeParse {
    public partial class LabelContext {

        bool CheckVarInitRValue(Node node) {
            if (Parse(CheckExpression, node)) { return true; }
            return false;
        }
        bool CheckVarInit(Node node) {
            if (!node.CheckTag("operator", "=")) { return false; }
            if (Parse(CheckVarInitRValue, node.UniqueLeaf())) { return true; }
            return false;
        }
        bool CheckAsModifier(Node node) {
            if (!node.CheckTag("operator", "as")) { return false; }
            if (node.Leaves.Count != 1) { Error(); }
            if (!Parse(CheckNumericLiteral, node.Leaves[0])) { Error(); }
            return true;
        }

        // TODO dont return true on CheckTag
        bool CheckInModifierValue(Node node) {
            if (Parse(CheckExpression, node)) { return true; }
            return false;
        }
        bool CheckInModifier(Node node) {
            if (!node.CheckTag("operator", "in")) { return false; }
            if (node.Leaves.Count != 1) { Error(); }
            if (!Parse(CheckInModifierValue, node.Leaves[0])) { Error(); }
            return true;
        }
        bool CheckIndexModifier(Node node) {
            if (!node.CheckTag("dot", "index")) { return false; }
            if (node.Leaves.Count == 0) { return true; }
            // todo procs can have more than numeric literals here, but objects maybe shouldnt
            if (node.Leaves.Count == 1) { return Parse(CheckExpression, node.Leaves[0]); }
            return Error();
        }
    }
}