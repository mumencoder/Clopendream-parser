﻿
namespace ClopenDream {

    public partial class LabelContext {

        public bool CheckPathTerminated(Node node) {
            if (node.Tags.ContainsKey("bare") && node.Leaves.Count == 0) { return true; }
            if (node.Leaves.Count > 0) {
                var has_blank = false;
                var all_blank = true;
                foreach (var leaf in node.Leaves) {
                    if (leaf.Tags.ContainsKey("blank")) { has_blank = true; }
                    else if (has_blank) { Error(); }
                    else {
                        all_blank = false;
                    }
                }
                return all_blank;
            } 
            return false;
        }

        public bool CheckPathTerminator(Node node) {
            if (Parse(CheckVarInit, node)) { return true; }
            if (Parse(CheckAsModifier, node)) { return true; }
            if (Parse(CheckIndexModifier, node)) { return true; }
            if (Parse(CheckInModifier, node)) { return true; }
            return Error();
        }

        public bool CheckPathDecl(Node node) {
            FixPathAllowedKeyword(node);
            if (Parse(CheckPathTerminated, node)) {
                if (!ParseLeaves(CheckPathTerminator, node)) { Error(); }
                return true;
            }
            else if (node.Tags.ContainsKey("bare")) {
                if (!ParseLeaves(CheckPathDecl, node)) { Error(); }
                return true;
            }
            else {
                return Error();
            }
        }

        //todo add all of them
        public bool FixPathAllowedKeyword(Node node) {
            if (node.CheckTag("operator", "in")) {
                node.Tags.Clear();
                node.Tags["bare"] = "in";
                return true;
            }
            if (node.CheckTag("operator", "to")) {
                node.Tags.Clear();
                node.Tags["bare"] = "to";
                return true;
            }
            if (node.CheckTag("operator", "step")) {
                node.Tags.Clear();
                node.Tags["bare"] = "step";
                return true;
            }
            if (node.CheckTag("keyword", "if")) {
                node.Tags.Clear();
                node.Tags["bare"] = "if";
                return true;
            }
            if (node.CheckTag("keyword", "do")) {
                node.Tags.Clear();
                node.Tags["bare"] = "do";
                return true;
            }
            if (node.CheckTag("keyword", "throw")) {
                node.Tags.Clear();
                node.Tags["bare"] = "throw";
                return true;
            }
            if (node.CheckTag("keyword", "try")) {
                node.Tags.Clear();
                node.Tags["bare"] = "try";
                return true;
            }
            if (node.CheckTag("keyword", "switch")) {
                node.Tags.Clear();
                node.Tags["bare"] = "switch";
                return true;
            }
            if (node.CheckTag("keyword", "spawn")) {
                node.Tags.Clear();
                node.Tags["bare"] = "spawn";
                return true;
            }
            if (!node.Tags.ContainsKey("bare") && !node.Tags.ContainsKey("operator")) { Error(); }
            return false;
        }
    }
}