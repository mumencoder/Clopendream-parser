
namespace ClopenDream {
    public partial class LabelContext {
        bool CheckExpression(Node node) {
            if (Parse(CheckSelfExpression, node)) { return true; }
            if (Parse(CheckOperatorExpression, node)) { return true; }
            if (Parse(CheckChainExpression, node)) { return true; }
            if (Parse(CheckLiteralExpression, node)) { return true; }
            if (Parse(CheckIndexExpression, node)) { return true; }
            if (Parse(CheckListExpression, node)) { return true; }
            if (Parse(CheckNewlistExpression, node)) { return true; }
            if (Parse(CheckArgListExpression, node)) { return true; }
            if (Parse(CheckPrePostExpression, node)) { return true; }
            if (Parse(CheckNewExpression, node)) { return true; }
            if (Parse(CheckIdentExpression, node)) { return true; }
            if (Parse(CheckCallExpression, node)) { return true; }
            if (Parse(CheckDynamicCallExpression, node)) { return true; }
            if (Parse(CheckEmptyExpression, node)) { return true; }
            if (Parse(CheckBuiltinExpression, node)) { return true; }
            if (Parse(CheckLotsaDots, node)) { return true; }
            if (Parse(CheckTupleExpression, node)) { return true; }
            return Error();
        }

        bool CheckLotsaDots(Node node) {
            return node.CheckTag("special", "......") || node.CheckTag("special", ".......");
        }

        int overflow = 0;
        bool CheckTupleExpression(Node node) {
            overflow++;
            if (overflow == 1000) {
                throw node.Trunk.Trunk.Error("overflow");
            }
            List<Node> stmts = NotActuallyCursed(node);
            if (stmts == null) { return false; }
            if (!ParseNodes(CheckExpression, stmts)) { Error(); }
            return true;
        }

        bool CheckChainExpression(Node node) {
            if (node.CheckTag("keyword", ".") && node.CheckTag("parens", "chain")) { return true; }
            return false;
        }
        bool CheckOperatorExpression(Node node) {
            if (!node.Tags.ContainsKey("operator")) { return false; }
            if (!ParseLeaves(CheckExpression, node)) { return false; }
            return true;
        }
        bool CheckPrePostExpression(Node node) {
            if (!node.CheckTag("dot", "post++") && !node.CheckTag("dot", "post--")) { return false; }
            if (node.Leaves.Count == 0) { return true; }
            else if (node.Leaves.Count != 1) { Error(); }
            if (!Parse(CheckExpression, node.Leaves[0])) { Error(); }
            return true;
        }

        bool CheckIdentExpression(Node node) {
            if (node.Tags.ContainsKey("ident")) { return true; }
            return false;
        }

        bool CheckIndexExpression(Node node) {
            if (!node.CheckTag("dot", "index")) { return false; }
            if (!ParseLeaves(CheckExpression, node)) { Error(); }
            return true;
        }
        bool CheckCallExpression(Node node) {
            if (!node.CheckTag("dot", "call")) { return false; }
            if (!ParseLeaves(CheckExpression, node)) { Error(); }
            return true;
        }
        bool CheckDynamicCallExpression(Node node) {
            if (!node.CheckTag("keyword", "call")) { return false; }
            if (node.Leaves.Count == 0) { Error(); }
            if (!ParseLeaves(CheckExpression, node)) { Error(); }
            return true;
        }
        bool CheckBuiltinExpression(Node node) {
            if (node.CheckTag("special", "global vars")) { return true; }
            if (!node.Tags.ContainsKey("bare")) { return false; }
            if (!Reserved.BuiltinProc.Contains((string)node.Tags["bare"])) { return false; }
            if (!ParseLeaves(CheckExpression, node)) { Error(); }
            return true;
        }

        bool CheckLiteralExpression(Node node) {
            if (Parse(CheckNumericLiteral, node)) { return true; }
            if (Parse(CheckStringLiteral, node)) { return true; }
            if (Parse(CheckResourceLiteral, node)) { return true; }
            if (Parse(CheckPathConstant, node)) { return true; }
            if (Parse(CheckNullLiteral, node)) { return true; }
            return false;
        }
        bool CheckSelfExpression(Node node) {
            if (!node.CheckTagArray("ident", 0, ".")) { return false; }
            if (node.Leaves.Count != 0) { Error(); }
            return true;
        }

        bool CheckEmptyExpression(Node node) {
            if (node.Tags.ContainsKey("blank") && node.Leaves.Count == 0) { return true; }
            return false;
        }
        bool CheckNullLiteral(Node node) {
            if (node.CheckTagArray("ident", 0, "null")) { return true; }
            return false;
        }

        bool CheckNumericLiteral(Node node) {
            if (node.Tags.ContainsKey("numeric")) { return true; }
            return false;
        }

        bool CheckStringLiteral(Node node) {
            if (!node.Tags.ContainsKey("string")) { return false; }
            if (node.Leaves.Count > 0) {
                if (!ParseLeaves(CheckExpression, node)) { Error(); }
            }
            return true;
        }

        bool CheckResourceLiteral(Node node) {
            if (!node.Tags.ContainsKey("resource")) { return false; }
            return true;
        }

        bool CheckPathConstant(Node node) {
            if (node.Tags.ContainsKey("path")) { return true; }
            return false;
        }

    }
}