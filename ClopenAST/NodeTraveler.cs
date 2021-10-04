
using System;
using System.Linq;
using System.Collections.Generic;

namespace ClopenDream {

    public partial class NodeTraveler {

        public Action<Node> VisitDefine;

        public void TravelRoot(Node root) {
            foreach (var leaf in root.Leaves) {
                TravelToplevel(leaf);
            }
        }

        public void TravelToplevel(Node n) 
        {
            if (n.Labels.Contains("ObjectDecl")) {
                foreach (var leaf in n.Leaves) {
                    TravelToplevel(leaf);
                }
            }
            else if (n.Labels.Contains("ObjectVarDecl")) {
                foreach (var leaf in n.Leaves) {
                    TravelPath(leaf);
                }
            }
            else if (n.Labels.Contains("ObjectAssignStmt")) {
                VisitDefine(n);
            }
            else if (n.Labels.Contains("ProcOverride")) {
                VisitDefine(n);
            }
            else if (n.Labels.Contains("ProcDecl")) {
                foreach (var leaf in n.Leaves) {
                    VisitDefine(leaf);
                }
            }
            else if (n.Labels.Contains("Proc")) {
                VisitDefine(n);
                if (n.Tags.ContainsKey("overload")) {
                }
                else {
                }
            }
            else if (n.Labels.Contains("ParentDecl")) {
                VisitDefine(n);
            }
            else if (n.Labels.Contains("ChildDecl")) {
                VisitDefine(n);
            }
            else {
                throw n.Error("TravelToplevel");
            }
        }

        public void TravelPath(Node n) {
            if (n.Labels.Contains("PathTerminated")) {
                VisitDefine(n);
            }
            else if (n.Labels.Contains("PathDecl")) {
                foreach (var leaf in n.Leaves) {
                    TravelPath(leaf);
                }
            }
            else {
                throw n.Error("TravelPath");
            }
        }
    }

}
