
using System;
using System.Collections.Generic;

namespace DMTreeParse {
    public class FixEmpty {

        public Node EmptyRoot, FixRoot;

        NodeTraveler nt = new();
        HashSet<string> empty_nodes = new();
        List<Node> delete_nodes = new();

        public FixEmpty(Node empty_node, Node fix_node) {
            EmptyRoot = empty_node;
            FixRoot = fix_node;
        }

        public void Begin() {
            nt.VisitDefine = this.EmptyVisit;
            nt.TravelRoot(EmptyRoot);
            nt.VisitDefine = this.FixVisit;
            nt.TravelRoot(FixRoot);
            foreach (var n in delete_nodes) {
                n.Trunk.Delete(n);
            }
            nt.VisitDefine = this.CheckFixVisit;
            nt.TravelRoot(FixRoot);
        }

        void EmptyVisit(Node n) {
            empty_nodes.Add( n.GetHash() );
        }

        void FixVisit(Node n) {
            string h = n.GetHash();
            if (empty_nodes.Contains( h )) {
                delete_nodes.Add(n);
            }
        }
        void CheckFixVisit(Node n) {
            string h = n.GetHash();
            if (empty_nodes.Contains(h)) {
                Console.WriteLine(n.PrintLeaves(1));
                throw new Exception();
            }
        }
    }
}
