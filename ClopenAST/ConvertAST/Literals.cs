
using OpenDreamShared.Dream;
using DMCompiler.Compiler.DM;

namespace ClopenDream {
    public partial class ConvertAST {
        DreamPath FlattenPath(Node node) {
            string[] path_elements = (string[])node.Tags["path"];
            return new DreamPath(path_elements.Aggregate((s1, s2) => s1 + s2));
        }

        DreamPath ConstPath(string[] segments, ref int i, bool start) {
            string s = "";
            bool path_sep = segments[i] == "/" || segments[i] == "." || segments[i] == ":";
            if (start && path_sep) {
                s = (segments[i] + segments[i + 1]);
                i += 2;
            } else if (segments[i] == "/") {
                i += 1;
            } else if (path_sep) {
                throw new Exception($"cannot parse {string.Join("", segments)} {i} {start}");
            } 

            while (i < segments.Length && segments[i] != ".") {
                s += segments[i];
                i += 1;
            }
            return new DreamPath(s);
        }
        DMASTExpressionConstant ConvertPath(Node node) {
            int i = 0;
            string[] segments = node.Tags["path"] as string[];
            DMASTExpressionConstant path = null;
            var constpath = ConstPath(segments, ref i, true);
            path = new DMASTConstantPath(node.Location, new DMASTPath(node.Location, constpath));
            while (i < segments.Length) {
                if (segments[i] == ".") {
                    i += 1;
                    path = new DMASTUpwardPathSearch(node.Location, path, new DMASTPath(node.Location, ConstPath(segments,ref i, false)));
                } else {
                    node.Error("Invalid path");
                }
            }
            return path;
        }

        DMASTExpression ConvertNumericLiteral(Node node, string s) {
            try {
                return new DMASTConstantInteger(node.Location, int.Parse(s));
            }
            catch (FormatException) { }
            try {
                return new DMASTConstantFloat(node.Location, float.Parse(s));
            }
            catch (FormatException) { }
            if (s == "inf") {
                return new DMASTConstantFloat(node.Location, float.PositiveInfinity);
            }
            if (s == "-inf") {
                return new DMASTConstantFloat(node.Location, float.NegativeInfinity);
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

        public string EscapeStringNew(string s) {
            if (s == null) { return s; }

            int write_pos = 0;
            int read_pos = 0;
            char[] cs = s.ToCharArray();
            for (read_pos = 0; read_pos < s.Length;) {
                char c = s[read_pos++];
                if (c == '\\') {
                    if (read_pos >= s.Length) {
                        break;
                    }
                    char c2 = s[read_pos++];
                    if (c2 == 'n') {
                        cs[write_pos++] = '\n';
                    } else if (c2 == 't') {
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
                        cs[write_pos++] = c;
                        cs[write_pos++] = c2;
                    }
                }
                else {
                    cs[write_pos++] = c;
                }
            }
            return new string(cs.Take<char>(write_pos).ToArray());
        }

        static string[] ignored_escapes = new string[] { "...", "improper", "b", "proper", "The", "the", "th", "an", "black", "red", "blue", "green", "Roman", "roman", "he", "she", "himself", "herself", "He", "She", "him", "her", "his", "hers", "His", "Her", "icon", "bold", "italic", "s", "a", "A" };
        string EscapeString(string s) {
            if (s == null) { return s; }

            int read_pos = 0;
            string new_s = "";
            for (read_pos = 0; read_pos < s.Length;) {
                char c = s[read_pos++];
                if (c == '\\') {
                    var found_ignored = false;
                    foreach (var ig in ignored_escapes) {
                        if (match(s, read_pos, ig)) {
                            new_s += '\\';
                            new_s += ig;
                            read_pos += ig.Length;
                            found_ignored = true;
                            break;
                        }
                    }
                    if (found_ignored) { continue; }
                    char c2 = s[read_pos++];
                    if (c2 == 'n') {
                        new_s += '\n';
                    } else if (c2 == 'u') {
                        new_s += 'u';
                        while (read_pos < s.Length && s[read_pos] >= 48 && s[read_pos] <= 57) {
                            new_s += s[read_pos];
                            read_pos++;
                        }
                    } else if (c2 == (char)255) {
                        new_s += (char)255;
                    } else if (c2 == 't') {
                        new_s += '\t';
                    } else if (c2 == 'r') {
                        new_s += '\r';
                    } else if (c2 == '\\') {
                        new_s += '\\';
                    } else if (c2 == '"') {
                        new_s += '"';
                    } else if (c2 == '\'') {
                        new_s += '\'';
                    } else if (c2 == '[') {
                        new_s += '[';
                    } else if (c2 == ']') {
                        new_s += ']';
                    } else if (c2 == '<') {
                        new_s += '<';
                    } else if (c2 == '>') {
                        new_s += '>';
                    } else if (c2 == ' ') {
                        new_s += ' ';
                    } else {
                        throw new Exception("unknown string escape " + s + " " + c + " " + c2);
                    }
                }
                else {
                    new_s += c;
                }
            }
            return new_s;
        }
    }
}