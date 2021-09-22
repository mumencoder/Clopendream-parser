
namespace DMTreeParse {
    public partial class LabelContext {
        bool CheckThrowStmt(Node node) {
            if (!node.CheckTag("keyword", "throw")) { return false; }
            if (!ParseLeaves(CheckExpression, node, length: 1)) { Error(); }
            return true;
        }
        bool CheckGotoStmt(Node node) {
            if (!node.CheckTag("keyword", "goto")) { return false; }
            return true;
        }
        bool CheckContinueStmt(Node node) {
            if (!node.CheckTag("keyword", "continue")) { return false; }
            return true;
        }
        bool CheckBreakStmt(Node node) {
            if (!node.CheckTag("keyword", "break")) { return false; }
            return true;
        }
        bool CheckReturnStmt(Node node) {
            if (!node.CheckTag("keyword", "return")) { return false; }
            if (node.Leaves.Count == 0) { return true; }
            if (ParseLeaves(CheckExpression, node, length: 1)) { return true; }
            return Error();
        }
        bool CheckSpawnStmt(Node node) {
            if (!node.CheckTag("keyword", "spawn")) { return false; }
            _continue_from = CheckSpawnBody;
            if (node.Leaves.Count == 0) { return true; }
            if (node.Leaves.Count != 1) { Error(); }
            if (!Parse(CheckExpression, node.Leaves[0])) { Error(); }
            return true;
        }
        bool CheckSpawnBody(Node node) {
            if (!Parse(CheckStmtBody, node)) { Error(); }
            return true;
        }

        bool CheckTryStmt(Node node) {
            if (!node.CheckTag("keyword", "try")) { return false; }
            if (!ParseStatements(CheckStatement, node.Leaves)) { Error(); }
            _continue_from = CheckTryVarDecl;
            return true;
        }

        bool CheckTryVarDecl(Node node) {
            if (Parse(CheckProcVarDeclStmt, node)) { _continue_from = CheckCatchStmt; return true; }
            else { _retry_from = CheckCatchStmt; return false; }
        }

        bool CheckCatchStmt(Node node) {
            if (!node.CheckTag("keyword", "catch")) { Error(); }
            _continue_from = CheckCatchBody;
            if (node.Leaves.Count == 0) { return true; }
            if (node.Leaves.Count == 1) {
                if (!Parse(CheckExpression, node.Leaves[0])) { Error(); }
                return true;
            }
            return Error();
        }

        bool CheckCatchBody(Node node) {
            var stmts = NotActuallyCursed(node);
            if (!ParseStatements(CheckStatement, stmts)) { Error(); }
            return true;
        }
    }
}