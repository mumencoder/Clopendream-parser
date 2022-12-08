
namespace ClopenDream {
    public partial class LabelContext {

        bool CheckStatement(Node node) {
            if (Parse(CheckProcVarDeclStmt, node)) { return true; }
            if (Parse(CheckThrowStmt, node)) { return true; }
            if (Parse(CheckSetStmt, node)) { return true; }
            if (Parse(CheckGotoStmt, node)) { return true; }
            if (Parse(CheckContinueStmt, node)) { return true; }
            if (Parse(CheckBreakStmt, node)) { return true; }
            if (Parse(CheckReturnStmt, node)) { return true; }
            if (Parse(CheckTryStmt, node)) { return true; }
            if (Parse(CheckIfStmt, node)) { return true; }
            if (Parse(CheckSwitchStmt, node)) { return true; }
            if (Parse(CheckForStmt, node)) { return true; }
            if (Parse(CheckWhileStmt, node)) { return true; }
            if (Parse(CheckDoWhileStmt, node)) { return true; }
            if (Parse(CheckSpawnStmt, node)) { return true; }
            if (Parse(CheckLabeledBlock, node)) { return true; }
            if (Parse(CheckExplicitBlock, node)) { return true; }
            if (Parse(CheckExpressionStmt, node)) { return true; }
            if (Parse(CheckImplicitBlock, node)) { return true; }
            if (Parse(CheckParentDecl, node)) { _parser.errors.Add("warning: parentdecl as proc statement"); return true; }
            return false;
        }

        bool CheckStmtBody(Node node) {
            List<Node> stmts = NotActuallyCursed(node);
            if (stmts == null) { return false; }
            if (!ParseStatements(CheckStatement, stmts)) { Error(); }
            return true;
        }

        bool CheckExpressionStmt(Node node) {
            if (Parse(CheckOperatorExpression, node)) { return true; }
            if (Parse(CheckNewExpression, node)) { return true; }
            if (Parse(CheckPrePostExpression, node)) { return true; }
            if (Parse(CheckChainExpression, node)) { return true; }
            if (Parse(CheckCallExpression, node)) { return true; }
            if (Parse(CheckDynamicCallExpression, node)) { return true; }
            if (Parse(CheckBuiltinExpression, node)) { return true; }
            return false;
        }

    }
}
