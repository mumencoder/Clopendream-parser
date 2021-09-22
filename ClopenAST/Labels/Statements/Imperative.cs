
using System;

namespace DMTreeParse {
    public partial class LabelContext {

        bool CheckProcVarDeclStmt(Node node) {
            if (!node.CheckTag("bare", "var")) { return false; }
            // TODO object and proc var decl paths could possibly be separated
            if (!ParseLeaves(CheckPathDecl, node)) { Error(); }
            return true;
        }


        bool CheckSetStmt(Node node) {
            if (!node.CheckTag("keyword", "set")) { return false; }
            if (node.Leaves.Count != 1) { Error(); }

            if (node.Leaves[0].CheckTag("operator", "in")) {
                if (node.Leaves[0].Leaves.Count != 2) { Error(); }
                if (!Parse(CheckIdentExpression, node.Leaves[0].Leaves[0])) { Error(); }
                if (!Parse(CheckExpression, node.Leaves[0].Leaves[1])) { Error(); }
                return true;
            }
            else if (node.Leaves[0].CheckTag("operator", "=")) {
                if (node.Leaves[0].Leaves.Count != 2) { Error(); }
                if (!Parse(CheckIdentExpression, node.Leaves[0].Leaves[0])) { Error(); }
                if (!Parse(CheckExpression, node.Leaves[0].Leaves[1])) { Error(); }
                return true;
            }
            return Error();
        }


        bool CheckExplicitBlock(Node node) {
            // TODO keywords can be labels, probably
            Node is_block = null;
            if (node.Tags.ContainsKey("bare") && node.Leaves.Count > 0 && node.Leaves[0].CheckTag("bare", "_block")) { 
                is_block = node.Leaves[0];
                node.Tags.Add("block", node.Tags["bare"]);
            }
            if (node.CheckTag("bare", "_block") && node.Leaves.Count > 0) { is_block = node; }
            if (is_block == null) { return false; }
            if (!ParseStatements(CheckStatement, is_block.Leaves)) { Error(); }
            return true;
        }
        bool CheckImplicitBlock(Node node) {
            // todo this is still sus
            if (node.Tags.ContainsKey("bare")) { node.Tags.Add("block", node.Tags["bare"]);  return true; }
            else { return false; }
        }
    }
}