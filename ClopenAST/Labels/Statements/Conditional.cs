
namespace DMTreeParse {
    public partial class LabelContext {

        bool CheckIfStmt(Node node) {
            if (!node.CheckTag("keyword", "if")) { return false; }
            if (!ParseLeaves(CheckExpression, node, length: 1)) { Error(); }
            _continue_from = CheckIfBody;
            return true;
        }

        bool CheckIfBody(Node node) {
            if (!Parse(CheckStmtBody, node, uncurse: false)) { Error(); }
            _continue_from = CheckElseBody;
            return true;
        }
        bool CheckElseBody(Node node) {
            if (!node.CheckTag("keyword", "else")) { _retry_from = CheckStatement; return false; }
            if (!ParseStatements(CheckStatement, node.Leaves)) { Error(); }
            return true;
        }

        bool CheckSwitchStmt(Node node) {
            if (!node.CheckTag("keyword", "switch")) { return false; }
            if (node.Leaves.Count != 1) { return Error(); }
            if (!Parse(CheckExpression, node.Leaves[0])) { Error(); }
            _continue_from = CheckSwitchBody;
            return true;
        }

        bool CheckSwitchBody(Node node) {
            var stmts = NotActuallyCursed(node);
            if (!ParseStatements(CheckSwitchBodyStmt, stmts)) { Error(); }
            return true;
        }

        bool CheckSwitchBodyStmt(Node node) {
            if (Parse(CheckSwitchIfHeader, node)) { _continue_from = CheckSwitchIfBody; return true; }
            //todo enforce node ending for ParseStatements
            if (Parse(CheckSwitchElseBody, node)) { return true; }
            return Error();
        }

        bool CheckSwitchIfHeader(Node node) {
            if (!node.CheckTag("keyword", "if")) { return false; }
            if (!ParseLeaves(CheckExpression, node)) { Error(); }
            return true;
        }

        bool CheckSwitchIfBody(Node node) {
            if (!Parse(CheckStmtBody, node)) { Error(); }
            return true;
        }

        bool CheckSwitchElseBody(Node node) {
            if (!node.CheckTag("keyword", "else")) { return false; }
            if (!ParseStatements(CheckStatement, node.Leaves)) { Error(); }
            _continue_from = CheckSwitchOptionalStmt;
            return true;
        }
        bool CheckSwitchOptionalStmt(Node node) {
            return Parse(CheckStatement, node);
        }
    }
}