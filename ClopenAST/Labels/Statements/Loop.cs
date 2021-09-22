
namespace DMTreeParse {
    public partial class LabelContext {
        bool CheckForStmt(Node node) {
            if (!node.CheckTag("keyword", "for")) { return false; }
            _continue_from = CheckForBody;
            if (node.Leaves.Count == 0) { return true; }
            if (node.Leaves.Count == 1) {
                if (!Parse(CheckStatement, node.Leaves[0]) && !Parse(CheckExpression, node.Leaves[0])) { Error(); }
                return true;
            }
            bool init_node = false, cond_node = false, stmt_node = false;
            if (node.Leaves.Count == 2) {
                init_node = cond_node = true;
            }
            if (node.Leaves.Count == 3) {
                init_node = cond_node = stmt_node = true;
            }
            if (init_node) {
                if (!Parse(CheckProcVarDeclStmt, node.Leaves[0]) && !Parse(CheckExpression, node.Leaves[0])) { Error(); }
            }
            if (cond_node) {
                if (!Parse(CheckExpression, node.Leaves[1])) { Error(); }
            }
            if (stmt_node) {
                if (!Parse(CheckStatement, node.Leaves[2])) { Error(); }
            }
            return true;
        }
        bool CheckForBody(Node node) {
            if (!Parse(CheckStmtBody, node, uncurse: false)) { Error(); }
            return true;
        }

        bool CheckWhileStmt(Node node) {
            if (!node.CheckTag("keyword", "while")) { return false; }
            if (node.Leaves.Count != 1) { Error(); }
            if (!Parse(CheckExpression, node.Leaves[0])) { Error(); }
            _continue_from = CheckWhileBody;
            return true;
        }

        bool CheckWhileBody(Node node) {
            if (!Parse(CheckStmtBody, node)) { Error(); }
            return true;
        }

        bool CheckDoWhileStmt(Node node) {
            if (!node.CheckTag("keyword", "do")) { return false; }
            if (!ParseStatements(CheckStatement, node.Leaves)) { Error(); }
            _continue_from = CheckDoWhileEnd;
            return true;
        }

        bool CheckDoWhileEnd(Node node) {
            if (!node.CheckTag("keyword", "while")) { Error(); }
            if (node.Leaves.Count != 1) { Error(); }
            if (!Parse(CheckExpression, node.Leaves[0])) { Error(); }
            return true;

        }
    }
}