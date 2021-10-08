﻿
using System;
using System.Linq;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;

namespace ClopenDream {
    public partial class ConvertAST {
        DreamPath ConvertPath(Node node) {
            string[] path_elements = (string[])node.Tags["path"];
            return new DreamPath(path_elements.Aggregate((s1, s2) => s1 + s2));
        }

        DMASTExpression ConvertNumericLiteral(string n) {
            try {
                return new DMASTConstantInteger(int.Parse(n));
            }
            catch (FormatException) { }
            try {
                return new DMASTConstantFloat(float.Parse(n));
            }
            catch (FormatException) { }
            if (n == "inf") {
                return new DMASTConstantFloat(float.PositiveInfinity);
            }
            if (n == "-inf") {
                return new DMASTConstantFloat(float.NegativeInfinity);
            }
            throw new Exception("GetExpression.NumericLiteral");
        }

        public bool match(string main, int start, string substr) {
            if (start + substr.Length > main.Length) { return false; }
            var end = start + substr.Length;
            var subi = 0;
            for (var i = start; i < end; i++, subi++) {
                if (main[i] != substr[subi]) { return false; }
            }
            return true;
        }

        string EscapeString(string s) {
            if (s == null) { return s; }

            int write_pos = 0;
            int read_pos = 0;
            char[] cs = s.ToCharArray();
            for (read_pos = 0; read_pos < s.Length;) {
                char c = s[read_pos++];
                if (c == '\\') {
                    char c2 = s[read_pos++];
                    // TODO b here is a bug in the \black escape
                    if (c2 == 'n') {
                        cs[write_pos++] = '\n';
                    }
                    else if (c2 == 't') {
                        cs[write_pos++] = '\t';
                    }
                    else if (c2 == '\\') {
                        cs[write_pos++] = '\\';
                    }
                    else if (c2 == '"') {
                        cs[write_pos++] = '\"';
                    }
                    else if (c2 == '\'') {
                        cs[write_pos++] = '\'';
                    }
                    else if (c2 == '[') {
                        cs[write_pos++] = '[';
                    }
                    else if (c2 == ']') {
                        cs[write_pos++] = ']';
                    }
                    else {
                        throw new Exception("unknown string escape " + s);
                    }
                }
                else {
                    cs[write_pos++] = c;
                }
            }
            return new string(cs.Take<char>(write_pos).ToArray());

            return s;
        }

        static string[] ignored_escapes = new string[] { "improper", "proper", "The", "red", "blue", "green", "Roman" };
        string EscapeStringOld(string s) {
            if (s == null) { return s; }

            int write_pos = 0;
            int read_pos = 0;
            char[] cs = s.ToCharArray();
            for (read_pos = 0; read_pos < s.Length;) {
                char c = s[read_pos++];
                if (c == '\\') {
                    var found_ignored = false;
                    foreach (var ig in ignored_escapes) {
                        if (match(s, read_pos, ig)) {
                            // TODO same as OD
                            read_pos += ig.Length;
                            found_ignored = true;
                            break;
                        }
                    }
                    if (found_ignored) { continue; }
                    char c2 = s[read_pos++];
                    // TODO b here is a bug in the \black escape
                    if (c2 == 'a' || c2 == 'b' || c2 == 'A') {
                        // TODO same as OD
                    }
                    else if (c2 == 's') {
                        // TODO same as OD
                    }
                    else if (c2 == 'n') {
                        cs[write_pos++] = '\n';
                    }
                    else if (c2 == 't') {
                        cs[write_pos++] = '\t';
                    }
                    else if (c2 == '\\') {
                        cs[write_pos++] = '\\';
                    }
                    else if (c2 == '"') {
                        cs[write_pos++] = '\"';
                    }
                    else if (c2 == '\'') {
                        cs[write_pos++] = '\'';
                    }
                    else if (c2 == '[') {
                        cs[write_pos++] = '[';
                    }
                    else if (c2 == ']') {
                        cs[write_pos++] = ']';
                    }
                    else {
                        throw new Exception("unknown string escape " + s);
                    }
                }
                else {
                    cs[write_pos++] = c;
                }
            }
            return new string(cs.Take<char>(write_pos).ToArray());
        }
    }
}