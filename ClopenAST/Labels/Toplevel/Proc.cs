
using System.Linq;

namespace ClopenDream {

    public partial class LabelContext {
        bool CheckProc(Node node) {
            if (!Parse(CheckProcHeader, node.Leaves[0])) { Error(); }
            if (!ParseStatements(CheckStatement, node.Leaves.Skip(1).TakeWhile((node) => true).ToList())) { Error(); }
            return true;
        }

        bool CheckProcHeader(Node node) {
            if (!node.CheckTag("bare", "var")) { return false; }
            if (node.Leaves.Count == 0) { return false; }
            Node arg_node = node.Leaves[0];
            if (!Parse(CheckArgGroup, arg_node)) { return false; }
            return true;
        }

        bool CheckArgGroup(Node node) {
            if (!node.CheckTag("dot", "arg")) { return false; }
            if (!ParseLeaves(CheckArgDecl, node)) { Error(); }
            return true;
        }

        bool CheckArgDecl(Node node) {
            FixPathAllowedKeyword(node);
            if (!Parse(CheckArgPathDecl, node)) { Error(); }
            return true;

        }

        bool CheckArgPathTerminator(Node node) {
            if (node.Tags.ContainsKey("blank")) {
                if (!ParseLeaves(CheckArgPathTerminator, node)) { Error(); }
                return true;
            }
            if (Parse(CheckVarInit, node)) { return true; }
            if (Parse(CheckAsModifier, node)) { return true; }
            if (Parse(CheckInModifier, node)) { return true; }
            return Error();
        }

        bool CheckArgPathDecl(Node node) {
            FixPathAllowedKeyword(node);
            if (Parse(CheckPathTerminated, node)) {
                if (node.Leaves.Count > 1) { Error(); }
                if (!ParseLeaves(CheckArgPathTerminator, node)) { Error(); }
                return true;
            }
            else if (node.Tags.ContainsKey("bare")) {
                if (!ParseLeaves(CheckArgPathDecl, node)) { Error(); }
                return true;
            }
            else {
                return Error();
            }
        }
    }
}