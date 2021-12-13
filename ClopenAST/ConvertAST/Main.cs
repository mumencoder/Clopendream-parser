using System;
using System.Collections.Generic;
using System.Linq;
using OpenDreamShared.Dream;
using DMCompiler.Compiler.DM;

namespace ClopenDream {
    public partial class ConvertAST {
        public Action<Node, DMASTNode> VisitDefine = ((n1, n2) => { });
        public Node ProcNode;
        Stack<Node> pathStack;
        Stack<DMASTExpression> derefExprStack = new();
        Stack<bool> derefExprCond = new();

        public Dictionary<Node, DMASTNode> closed_to_clopen_node = new();
        public Dictionary<DMASTNode, Node> clopen_to_closed_node = new();

        void AssociateNodes(Node n, DMASTNode node) {
            closed_to_clopen_node[n] = node;
            clopen_to_closed_node[node] = n;
        }
        string ExtractNodePath(Node n) {
            if (n.Tags.ContainsKey("bare")) {
                return (string)n.Tags["bare"];
            }
            if (n.Tags.ContainsKey("ident")) {
                var strs = ((string[])n.Tags["ident"]);
                if (strs.Length != 1) {
                    throw new Exception("non bare ident in path extraction");
                }
                return strs[0];
            }
            if (n.Tags.ContainsKey("overload")) {
                return (string)n.Tags["operator"];
            }
            throw n.Error("bad node path");
        }

        OpenDreamShared.Dream.Procs.DMValueType ConvertDMValueType(Node n) {
            var i = int.Parse(n.Tags["numeric"] as string);
            int result = 0;
            if ((i & 0x01) == 0x01) {
                result |= (int)OpenDreamShared.Dream.Procs.DMValueType.Mob;
            }
            if ((i & 0x02) == 0x02) {
                result |= (int)OpenDreamShared.Dream.Procs.DMValueType.Obj;
            }
            if ((i & 0x04) == 0x04) {
                result |= (int)OpenDreamShared.Dream.Procs.DMValueType.Text;
            }
            if ((i & 0x08) == 0x08) {
                result |= (int)OpenDreamShared.Dream.Procs.DMValueType.Num;
            }
            if ((i & 0x20) == 0x20) {
                result |= (int)OpenDreamShared.Dream.Procs.DMValueType.Turf;
            }
            if ((i & 0x80) == 0x80) {
                result |= (int)OpenDreamShared.Dream.Procs.DMValueType.Null;
            }
            if ((i & 0x800) == 0x800) {
                result |= (int)OpenDreamShared.Dream.Procs.DMValueType.Message;
            }
            if ((i & 0x1000) == 0x1000) {
                result |= (int)OpenDreamShared.Dream.Procs.DMValueType.Anything;
            }
            if ((i & 0x20000) == 0x20000) {
                result |= (int)OpenDreamShared.Dream.Procs.DMValueType.Color;
            }
            return (OpenDreamShared.Dream.Procs.DMValueType)result;
        }

        // TODO this should not be
        DMASTExpression NullifyNull(DMASTExpression expr) {
            if (expr is DMASTConstantNull) {
                return null;
            }
            return expr;
        }

        // TODO only one pass is correct
        string FormatText(string s) {
            s = s.Replace("\\ref[]", "ÿ" + (char)0x01);
            s = s.Replace("[]", "ÿ" + (char)0x00);
            return s;
        }
        DreamPath ExtractPath(Stack<Node> pathStack) {
            return new DreamPath(DreamPath.PathType.Absolute, pathStack.Select((node) => ExtractNodePath(node)).Reverse().ToArray());
        }

    }
}
