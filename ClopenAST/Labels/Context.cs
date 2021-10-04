
using System;
using System.Collections.Generic;


namespace ClopenDream {
    public class LabelException : Exception {

        LabelContext Context;
        Node Node;
        public LabelException(LabelContext ctx, Node n) {
            Context = ctx;
            Node = n;
        }
    }

    public partial class LabelContext {

        private int maxLookback = 64;
        private Queue<string> lookback = new();
        public bool IsDebugLine(int line) {
            return true;
        }
        public void Lookback(string s) {
            lookback.Enqueue(s);
            if (lookback.Count > maxLookback) {
                lookback.Dequeue();
            }
        }

        Stack<Node> _debugStack = new();

        //TODO does optional get used?
        public bool Parse(Func<Node, bool> parser, Node node, bool uncurse = true, bool label = true, bool optional = false) {
            if (uncurse) { while (node != Uncurse(node)) { node = Uncurse(node); } }
            _debugStack.Push(node);
            if (IsDebugLine(node.RawLine)) {
                Lookback(node.RawLine + " " + parser.Method.Name + " enter ");
            }
            bool result = parser(node);
            if (IsDebugLine(node.RawLine)) {
                Lookback(node.RawLine + " " + parser.Method.Name + " leave is " + result);
            }
            if (result == true && label) {
                node.Labels.Add(parser.Method.Name);
            }
            else if (!label) {
                node.ClearLabels();
            }
            else {
                if (!optional) { node.ClearLabels(); }
            }
            _debugStack.Pop();
            return result;
        }

        Func<Node, bool> _continue_from;
        Func<Node, bool> _retry_from;

        public bool ParseStatements(Func<Node, bool> parser, List<Node> nodes) {
            _continue_from = null;
            _retry_from = null;
            foreach (var leaf in nodes) {
                if (_continue_from != null) {
                    var continue_parser = _continue_from;
                    _continue_from = null;
                    Parse(continue_parser, leaf, uncurse: false);
                    if (_retry_from != null) {
                        var retry_parser = _retry_from;
                        _retry_from = null;
                        Parse(retry_parser, leaf, uncurse: false);
                    }
                    continue;
                }
                if (!Parse(parser, leaf)) { Error(); }
            }
            _continue_from = null;
            _retry_from = null;
            return true;
        }

        public bool ParseLeaves(Func<Node, bool> parser, Node node, bool label = true, bool optional = false, int length = -1) {
            return ParseNodes(parser, node.Leaves, label: label, optional: optional, length: length);
        }
        public bool ParseNodes(Func<Node, bool> parser, List<Node> nodes, bool label = true, bool optional = false, int length = -1) {
            if (length > -1 && nodes.Count != length) { return false; }
            foreach (var leaf in nodes) {
                if (!Parse(parser, leaf, label: label, optional: optional)) { return false; }
            }
            return true;
        }

        public bool Error() {
            foreach (var s in lookback) {
                Console.WriteLine(s);
            }
            Console.WriteLine(_debugStack.Pop().PrintLeaves(3));
            throw new LabelException(this, _debugStack.Pop());
        }
    }
}