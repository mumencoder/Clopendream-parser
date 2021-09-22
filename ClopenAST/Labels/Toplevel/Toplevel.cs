
using System;
using System.Collections.Generic;

namespace DMTreeParse{

    public partial class LabelContext {
        public bool CheckTopLevel(Node node) {
            foreach (var leaf in node.Leaves) {
                if (Parse(CheckProcDecl, leaf)) { continue; }
                if (Parse(CheckProcOverride, leaf)) { continue; }
                if (Parse(CheckObjectVarDecl, leaf)) { continue; }
                if (Parse(CheckObjectDecl, leaf)) { continue; }
                return Error();
            }
            return true;
        }

        bool CheckProcDecl(Node node) {
            if (!node.CheckTag("bare", "proc")) { return false; }
            if (node.Leaves.Count == 0) { return Error(); }
            if (!ParseLeaves(CheckProc, node)) { return Error(); }
            return true;
        }
        bool CheckProcOverride(Node node) {
            if (!node.Tags.ContainsKey("bare")) { return false; }
            if (node.CheckTag("bare", "proc")) { return false; }
            if (node.Leaves.Count == 0) { return false; }
            Node proc_header_node = node.Leaves[0];
            if (proc_header_node == null) { return false; }
            if (!Parse(CheckProcHeader, proc_header_node, label: false)) { return false; }
            if (!Parse(CheckProc, node)) { return Error(); }
            return true;
        }
        bool CheckObjectVarDecl(Node node) {
            if (!node.CheckTag("bare", "var")) { return false; }
            if (!ParseLeaves(CheckPathDecl, node)) { Error(); }
            return true;
        }
        bool CheckObjectDecl(Node node) {
            foreach (var leaf in node.Leaves) {
                if (Parse(CheckProcDecl, leaf)) { continue; }
                if (Parse(CheckProcOverride, leaf)) { continue; }
                if (Parse(CheckObjectVarDecl, leaf)) { continue; }
                if (Parse(CheckObjectAssignStmt, leaf)) { continue; }
                if (Parse(CheckParentDecl, leaf)) { continue; }
                if (Parse(CheckChildDecl, leaf)) { continue; }
                if (Parse(CheckObjectDecl, leaf)) { continue; }
                Error();
            }
            return true;
        }

    }
}