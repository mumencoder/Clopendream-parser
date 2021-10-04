using System;
using System.Collections.Generic;
using System.Linq;
using OpenDreamShared.Dream;
using OpenDreamShared.Compiler.DM;

namespace ClopenDream {
    public partial class ConvertAST {

        DMASTCallParameter GetCallParameter(Node node) {
            if (node.Tags.ContainsKey("blank") && node.Leaves.Count == 1) {
                node = node.Leaves[0];
            }
            DMASTExpression expr = null;
            string name = null;
            if (node.Labels.Contains("ListAssign")) {
                if (node.Leaves[0].Tags.ContainsKey("string")) {
                    name = (string)node.Leaves[0].Tags["string"];
                    expr = GetExpression(node.Leaves[1]);
                }
                else {
                    expr = new DMASTAssign(GetExpression(node.Leaves[0]), GetExpression(node.Leaves[1]));
                }
            }
            else {
                expr = GetExpression(node);
            }
            // note names can be escaped?
            return new DMASTCallParameter(expr, EscapeString(name));
        }

        DMASTCallParameter[] GetCallParameters(List<Node> nodes) {
            if (nodes.Count == 0) {
                return new DMASTCallParameter[0];
            }
            //foreach (var node in nodes) {
            //    Console.WriteLine("callp");
            //    Console.WriteLine( node.PrintLeaves(10) );
            //}
            var param_nodes = MatchArgListList(nodes[0]);
            if (param_nodes == null) {
                param_nodes = nodes;
            }
            return param_nodes.Select((node) => GetCallParameter(node)).ToArray();
        }

        List<Node> MatchArgListList(Node node) {
            if (node.Tags.ContainsKey("blank") && node.Leaves.Count == 1) {
                node = node.Leaves[0];
            }
            else { return null; }
            if (node.Labels.Contains("ArgListExpression")) {
                node = node.Leaves[0];
            }
            else { return null; }
            if (node.Tags.ContainsKey("blank") && node.Leaves.Count == 1) {
                node = node.Leaves[0];
            }
            else { return null; }
            if (node.Labels.Contains("ListExpression")) {
                return node.Leaves;
            }
            return null;
        }
    }
}
