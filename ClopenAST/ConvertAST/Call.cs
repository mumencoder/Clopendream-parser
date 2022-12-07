using DMCompiler.Compiler.DM;

namespace ClopenDream {
    public partial class ConvertAST {

        DMASTCallParameter GetCallParameter(Node node) {
            if (node.Tags.ContainsKey("blank") && node.Leaves.Count == 1) {
                node = node.Leaves[0];
            }
            DMASTExpression expr = null;
            DMASTExpression key = null;
            string name = null;
            if (node.Labels.Contains("ListAssign")) {
                if (node.Leaves[0].Tags.ContainsKey("string")) {
                    key = new DMASTConstantString( node.Location, (string)node.Leaves[0].Tags["string"]);
                    expr = GetExpression(node.Leaves[1]);
                }
                else {
                    key = GetExpression(node.Leaves[0]);
                    expr = GetExpression(node.Leaves[1]);
                }
                return new DMASTCallParameter(node.Location, expr);
            } else {
                expr = GetExpression(node);
                return new DMASTCallParameter(node.Location, expr);
            }
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
