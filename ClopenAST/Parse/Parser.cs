using System;
using System.IO;
using System.Collections.Generic;
using OpenDreamShared.Compiler;

namespace ClopenDream {

    public partial class Parser {

        private List<string> _topLevel = new();
        private string _cText = "";
        private int _cLineNumber = 0;
        private int _cPos = 0;
        private TextReader _input;
        private Stack<Node> _node_stack = new();

        private bool eol(int n = 0) {
            return !(_cPos + n < _cText.Length);
        }
        private int left() {
            return _cText.Length - _cPos;
        }
        private char getc(int n = 0) {
            return _cText[_cPos + n];
        }
        private void NextLine() {
            _cText = _input.ReadLine();
            _cPos = 0;
            _cLineNumber += 1;
        }
        private int ReadIndent() {
            int indent = 0;
            while (!eol() && getc() == '\t') {
                indent++;
                _cPos += 1;
            }
            return indent;
        }
        public Node BeginParse(System.IO.TextReader input, FileInfo original_file = null) {
            _input = input;

            var root = new Node();
            root.Indent = -1;
            root.Tags.Add("root", "");
            _node_stack.Push(root);

            do {
                NextLine();
                if (_cText == null) { break; }
                while (left() == 0) { NextLine(); }
                string topLevel = ParseTopLevel();
                if (topLevel != null) {
                    _topLevel.Add(topLevel);
                    continue;
                }

                Node parentNode = _node_stack.Peek();
                var indent = ReadIndent();
                Node cNode;
                try {
                    cNode = ReadNode();
                }
                catch (Exception) {
                    Console.WriteLine("Exception at line: " + _cLineNumber + " |" + _cText);
                    throw;
                }
                if (cNode == null) {
                    Console.WriteLine("unknown node type " + _cLineNumber + " |" + _cText);
                    return null;
                }

                skipws();

                var lpos = _cPos;
                Location? l = ReadLocation();
                if (l == null) {
                    Console.WriteLine("bad location " + _cLineNumber + " |" + _cText.Substring(lpos));
                }
                else {
                    cNode.Location = l.Value;
                }
                cNode.RawLine = _cLineNumber;
                cNode.Text = _cText;
                cNode.Indent = indent;

                if (cNode.Indent == parentNode.Indent) {
                    _node_stack.Pop();
                    _node_stack.Peek().Leaves.Add(cNode);
                    _node_stack.Push(cNode);
                }
                else if (cNode.Indent == parentNode.Indent + 1) {
                    parentNode.Leaves.Add(cNode);
                    _node_stack.Push(cNode);
                }
                else if (cNode.Indent < parentNode.Indent) {
                    while (cNode.Indent < parentNode.Indent) {
                        parentNode = _node_stack.Pop();
                    }
                    _node_stack.Peek().Leaves.Add(cNode);
                    _node_stack.Push(cNode);
                }
                else {
                    throw new Exception("invalid indent level " + cNode.Indent + " " + parentNode.Indent + " " + _cLineNumber);
                }


            } while (true);

            var ctx = new LabelContext();
            root.Connect();
            ctx.CheckTopLevel(root);
            return root;
        }

        public Node ReadNode() {
            Node nni = null;
            int start = _cPos;

            var until_spess = reset(() => read_until(' '));

            _cPos = start;
            nni = ReadIrregular();
            if (nni != null) {
                return nni;
            }
            _cPos = start;
            nni = ReadText();
            if (nni != null) {
                return nni;
            }
            _cPos = start;
            nni = ReadKeyword(until_spess);
            if (nni != null) {
                return nni;
            }
            _cPos = start;
            nni = ReadOperator(until_spess);
            if (nni != null) {
                return nni;
            }
            _cPos = start;
            nni = ReadOverload();
            if (nni != null) {
                return nni;
            }
            _cPos = start;
            nni = ReadBare();
            if (nni != null) {
                return nni;
            }
            _cPos = start;
            nni = ReadParen();
            if (nni != null) {
                return nni;
            }
            _cPos = start;
            nni = ReadDot();
            if (nni != null) {
                return nni;
            }
            _cPos = start;
            nni = ReadBlank();
            if (nni != null) {
                return nni;
            }
            return null;
        }

        public Node ReadIrregular() {
            if (match(_cText, _cPos, ". (chain)")) {
                _cPos += ". (chain)".Length;
                var node = new Node();
                node.Tags.Add("operator", ".");
                return node;
            }
            return null;
        }

        public Node ReadText() {
            if (match(_cText, _cPos, "text")) {
                _cPos += 4;
                if (getc() != ' ' || getc(1) != '(') { return null; }
                _cPos += 2;
                if (getc() == '"') {
                    var str = read_string();
                    if (getc() != ')') { return null; }
                    _cPos += 1;
                    var node = new Node();
                    node.Tags.Add("text", "");
                    node.Tags.Add("string", str);
                    return node;
                }

            }
            return null;
        }

        public Node ReadKeyword(string until_spess) {
            if (!is_alpha(getc()) && getc() != '.') { return null; }
            if (Reserved.Keywords.Contains(until_spess)) {
                _cPos += until_spess.Length;
                var node = new Node();
                node.Tags.Add("keyword", until_spess);
                if (!Reserved.ParenKeywords.Contains(until_spess)) { return node; }
                skipws();
                if (has_parens()) {
                        var parens = read_parens(ExprParens);
                        node.Tags.Add(parens.Item1, parens.Item2);
                }
                return node;
            }
            return null;
        }
        public Node ReadOperator(string until_spess) {
            if (Reserved.Operators.Contains(until_spess)) {
                _cPos += until_spess.Length;
                var node = new Node();
                node.Tags.Add("operator", until_spess);
                return node;
            }
            return null;
        }

        public Node ReadOverload() {
            if (match(_cText, _cPos, "operator")) {
                _cPos += 8;
                var until_spess = reset( () => read_until(' ') );
                var node = ReadOperator(until_spess);
                if (node == null) { return null; }
                node.Tags.Add("overload", "");
                return node;
            }
            return null;
        }
        public Node ReadBare() {
            var start = _cPos;
            if (is_ident_start(getc())) {
                var alnum = read_ident();
                var node = new Node();
                node.Tags.Add("bare", alnum);
                return node;
            }
            return null;
        }
        public Node ReadParen() {
            Node node;
            if (match(_cText, _cPos, "(global vars)")) {
                _cPos += "(global vars)".Length;
                node = new Node();
                node.Tags.Add("special", "global vars");
                return node;
            }
            if (getc() != ' ' || getc(1) != '(') { return null; }
            _cPos += 2;
            if (match(_cText, _cPos, "inf)")) {
                _cPos += "inf".Length;
                node = new Node();
                node.Tags.Add("numeric", "inf");
            }
            else if (match(_cText, _cPos, "-inf)")) {
                _cPos += "-inf".Length;
                node = new Node();
                node.Tags.Add("numeric", "-inf");
            }
            else if (match(_cText, _cPos, "+inf)")) {
                _cPos += "+inf".Length;
                node = new Node();
                node.Tags.Add("numeric", "+inf");
            }
            else if (match(_cText, _cPos, ".......)")) {
                _cPos += 7;
                node = new Node();
                node.Tags.Add("special", ".......");
            }
            else if (match(_cText, _cPos, "......)")) {
                _cPos += 6;
                node = new Node();
                node.Tags.Add("special", "......");
            }
            else if (match(_cText, _cPos, "..)")) {
                _cPos += 2;
                node = new Node();
                node.Tags.Add("special", "..");
            }
            else if (match(_cText, _cPos, ".)")) {
                _cPos += 1;
                node = new Node();
                node.Tags.Add("ident", new string[] { "." });
            }
            else if (left() >= 1 && is_digit(getc()) || getc() == '-') {
                var num = read_numeric();
                node = new Node();
                node.Tags.Add("numeric", num);
            }
            else if (left() >= 1 && getc() == '"') {
                var str = read_string();
                node = new Node();
                node.Tags.Add("string", str);
            }
            else if (left() >= 1 && getc() == '\'') {
                var str = read_resource();
                node = new Node();
                node.Tags.Add("resource", str);
            }
            else if (left() >= 1 && is_path_separator(getc())) {
                var path = ExpressionPath(1);
                node = new Node();
                node.Tags.Add("path", path);
            }
            else if (left() >= 1 && is_deref_start(getc())) {
                var deref = ExpressionDereference();
                node = new Node();
                node.Tags.Add("ident", deref);
            }
            else {
                return null;
            }
            if (getc() != ')') { return null; }
            _cPos += 1;
            return node;
        }

        public Node ReadDot() {
            if (getc() != '.') { return null; }
            _cPos += 1;
            var dotstr = read_until(' ');
            if (Reserved.DotTypes.Contains(dotstr)) {
                var node = new Node();
                node.Tags.Add("dot", dotstr);

                if (Reserved.ParenDotTypes.Contains(dotstr)) {
                    skipws();
                    if (has_parens()) {
                        if (dotstr == "call" || dotstr == "index") {
                            var parens = read_parens(CallParens);
                            node.Tags.Add(parens.Item1, parens.Item2);
                        }
                        else if (dotstr == "post++" || dotstr == "post--" || dotstr == "child_type") {
                            var parens = read_parens(ExprParens);
                            node.Tags.Add(parens.Item1, parens.Item2);
                        }
                        else {
                            node.Tags.Add("parens", read_parens());
                        }
                    }
                }
                return node;
            }
            return null;
        }

        public Node ReadBlank() {
            if (getc() == ' ') { 
                _cPos++;
                var node = new Node();
                node.Tags.Add("blank", "");
                return node;
            }
            return null;
        }
        public OpenDreamShared.Compiler.Location? ReadLocation() {
            if (getc() != '[') {
                return null;
            }
            _cPos += 1;
            var filestart = _cPos;
            while (getc() != ':' && getc() != '\n') {
                _cPos += 1;
            }
            string text = _cText.Substring(filestart, _cPos - filestart);
            if (getc() != ':') { return null; }
            _cPos += 1;
            var linestart = _cPos;
            while (getc() != ']' && getc() != '\n') {
                _cPos += 1;
            }
            if (getc() != ']') { return null; }
            int line = int.Parse( _cText.Substring(linestart, _cPos - linestart) );

            return new OpenDreamShared.Compiler.Location(text, line, null);
        }

    }
}