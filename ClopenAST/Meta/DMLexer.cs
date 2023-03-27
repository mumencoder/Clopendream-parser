
namespace ClopenDream {
    public class DMLexer {
        CombinedSource _tp;
        SourceLocation start_location;
        Queue<DMToken> pending_tokens = new();

        public DMLexer() {
            _tp = new CombinedSource();
            start_location = _tp.CurrentLocation();
        }

        // Token queue output
        public void CreateToken(DMToken.Kind ty, object value = null) {
            pending_tokens.Enqueue(new DMToken(ty, value));
        }
        public void AcceptToken(DMToken.Kind ty, int n) {
            if (n == 0) {
                throw new Exception("no progress");
            }
            var text = _tp.GetString(n);
            _tp.Advance(n);
            var end_location = _tp.CurrentLocation();
            end_location.Position -= 1;
            DMToken token = new DMToken(ty, text, new SourceSpan(start_location, end_location));
            start_location = _tp.CurrentLocation();
            pending_tokens.Enqueue(token);
        }
        public DMToken NextToken() {
            while (pending_tokens.Count == 0) {
                Advance();
            }
            return pending_tokens.Dequeue();
        }

        // Position control
        public void Include(SourceText srctext) {
            _tp.Include(srctext);
        }
        public void SavePosition() {
            _tp.SavePosition();
        }
        public void RestorePosition() {
            _tp.RestorePosition();
        }
        public void AcceptPosition() {
            _tp.AcceptPosition();
        }

        // Matching
        public static bool IsIdentifierStart(char c) {
            return (IsAlphabetic(c) || c == '_');
        }
        public static bool IsIdentifier(char c) {
            return (IsAlphabetic(c) || IsNumeric(c) || c == '_');
        }
        public static bool IsAlphabetic(char c) {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        public static bool IsNumeric(char c) {
            return (c >= '0' && c <= '9');
        }

        public static bool IsAlphanumeric(char c) {
            return IsAlphabetic(c) || IsNumeric(c);
        }

        public static bool IsHex(char c) {
            return IsNumeric(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }

        public string ReadLine() {
            int start = _tp.CurrentPosition();
            while (_tp.Peek(0) != '\n') {
                _tp.Advance(1);
            }
            if (_tp.CurrentPosition() == start) { return ""; }
            return _tp.GetString(start, _tp.CurrentPosition() - 1);
        }

        public void Advance() {
            char? c = _tp.Peek(0);
            switch (c) {
                case null: CreateToken(DMToken.Kind.EndOfFile); break;
                case ' ': AcceptToken(DMToken.Kind.Whitespace, 1); break;
                case '\t': AcceptToken(DMToken.Kind.Whitespace, 1); break; 
                case '\n': AcceptToken(DMToken.Kind.Newline, 1); break;
                case '.': ReadDots(); break;
                case '[':
                case ']':
                case ';':
                case ':':
                case ',':
                case '(':
                case ')': AcceptToken(DMToken.Kind.Symbol, 1); break;
                case '>':
                    switch (_tp.Peek(1)) {
                        case '>':
                            if (_tp.Peek(2) == '=') { AcceptToken(DMToken.Kind.Symbol, 3); } 
                            else { AcceptToken(DMToken.Kind.Symbol, 2); }
                            break;
                        case '=': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '?': 
                    switch (_tp.Peek(1)) {
                        case '.': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        case ':': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        case '[': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '<': 
                    switch (_tp.Peek(1)) {
                        case '<':
                            if (_tp.Peek(2) == '=') { AcceptToken(DMToken.Kind.Symbol, 3); } 
                            else { AcceptToken(DMToken.Kind.Symbol, 2); }
                            break;
                        case '=': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '|':
                    switch (_tp.Peek(1)) {
                        case '|':
                            switch (_tp.Peek(2)) {
                                case '=': AcceptToken(DMToken.Kind.Symbol, 3); break;
                                default: AcceptToken(DMToken.Kind.Symbol, 2); break;
                            }
                            break;
                        case '=': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '*':
                    switch (_tp.Peek(1)) {
                        case '*': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        case '=': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '+':
                    switch (_tp.Peek(1)) {
                        case '+': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        case '=': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '-':
                    switch (_tp.Peek(1)) {
                        case '=': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        case '-': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '&':
                    switch (_tp.Peek(1)) {
                        case '&': 
                            switch (_tp.Peek(2)) {
                                case '=': AcceptToken(DMToken.Kind.Symbol, 3); break;
                                default: AcceptToken(DMToken.Kind.Symbol, 2); break;
                            }
                            break;
                        case '=': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '~':
                    switch (_tp.Peek(1)) {
                        case '=': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        case '!': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '%':
                    switch (_tp.Peek(1)) {
                        case '=': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '^':
                    switch (_tp.Peek(1)) {
                        case '=': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '!': 
                    switch (_tp.Peek(1)) {
                        case '=': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '=':
                    switch (_tp.Peek(1)) {
                        case '=': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '\\':
                    switch (_tp.Peek(1)) {
                        case '\n': AcceptToken(DMToken.Kind.Whitespace, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '/':
                    switch (_tp.Peek(1)) {
                        case '/': SkipSingleComment(); break;
                        // note: C turns comments into whitespace tokens but this seems to mess with DM's whitespace sensitivity
                        case '*': SkipMultiComment(); break;
                        case '=': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                case '@': LexRawString(); break;
                case '\'':
                case '"':  LexString(false); break;
                case '{': 
                    if (_tp.Peek(1) == '"') {
                        LexString(true);
                    } else {
                        AcceptToken(DMToken.Kind.Symbol, 1);
                    }
                    break;
                case '}': AcceptToken(DMToken.Kind.Symbol, 1); break;
                case '#': 
                    switch (_tp.Peek(1)) {
                        case '#': AcceptToken(DMToken.Kind.Symbol, 2); break;
                        default: AcceptToken(DMToken.Kind.Symbol, 1); break;
                    }
                    break;
                default: 
                    char cc = (char)c;
                    if (IsIdentifierStart(cc)) {
                        ReadIdentifier();
                    } else if (IsNumeric(cc)) {
                        ReadNumeric();
                    } else {
                        throw new Exception("Unknown Preprocessing character " + c);
                    }
                    break;
            }
        }

        public void ReadIdentifier() {
            int n = 0;
            while (true) {
                char? c = _tp.Peek(n);
                if (c is char cc && IsIdentifier(cc)) {
                    n += 1;
                    continue;
                }
                break;
            }
            AcceptToken(DMToken.Kind.Identifier, n);
        }

        public void ReadNumeric() {
            int n = 0;
            string error = null;
            while (true) {
                char? c = _tp.Peek(n);
                if (c == '#') {
                    if (_tp.Match("INF", n + 1)) {
                        n += 4;
                        break;
                    }
                    error = "Invalid # modifier in numeric literal";
                    break;
                }
                if (c == 'x' || c == 'X') {
                    n += 1;
                    continue;
                }
                if (c == 'e' || c == 'E') {
                    c = _tp.Peek(n + 1);
                    if (c == '-' || c == '+') {
                        n += 2;
                        continue;
                    } else {
                        n += 1;
                        continue;
                    }
                } else if (c is char cc && IsHex(cc) || c == '.' || c == 'p' || c == 'P') {
                    n += 1;
                    continue;
                } else {
                    break;
                }
            }
            if (error != null) {
                throw new Exception("Error in numeric literal: " + error);
            }
            AcceptToken(DMToken.Kind.Numeric, n);
        }

        public void ReadDots() {
            int n = 0;
            while (_tp.Peek(n) == '.') {
                n += 1;
            }
            AcceptToken(DMToken.Kind.Symbol, n);
        }

        public void Splice() {
            bool isSpliceSkip(char? c) {
                return c == ' ' || c == '\t' || c == '\n';
            }
            int n = 0;
            while (true) {
                char? c = _tp.Peek(n);
                if (!isSpliceSkip(c)) {
                    break;
                } else {
                    n += 1;
                }
            }
            _tp.Advance(n);
        }
        public void SkipMultiComment() {
            int n = 0;
            int nestedComments = 0;
            while (true) {
                char? c = _tp.Peek(n);
                if (c == '/' && _tp.Peek(n + 1) == '/') {
                    _tp.Advance(n);
                    n = 0;
                    SkipSingleComment();
                    continue;
                }
                if (c == '/' && _tp.Peek(n + 1) == '*') { n += 2; nestedComments++; } else if (c == '*' && _tp.Peek(n + 1) == '/') {
                    n += 2;
                    if (nestedComments > 0) { nestedComments--; } else { break; }
                } else if (c == null) throw new Exception("Expected \"*/\" to end multiline comment");
                else { n += 1; }
            }
            while (true) {
                if (_tp.Peek(n) == ' ' || _tp.Peek(n) == '\t') {
                    n += 1;
                } else {
                    break;
                }
            }
            AcceptToken(DMToken.Kind.Whitespace, n);
        }
        public void SkipSingleComment() {
            int n = 0;
            while (true) {
                char? c = _tp.Peek(n);
                if (c != '\n') {
                    n += 1;
                    continue;
                }
                break;
            }
            AcceptToken(DMToken.Kind.Whitespace, n);
        }

        public void LexRawString() {
            char? delimiter = null;
            string text_delimiter = null;
            bool is_long = false;
            if (_tp.Peek(0) == '{') {
                if (_tp.Peek(1) == '"') {
                    delimiter = '"';
                    is_long = true;
                    if (_tp.Peek(2) == '\n') {
                        _tp.Advance(3);
                    } else {
                        _tp.Advance(2);
                    }
                }
            } else if (_tp.Peek(0) == '(') {
                _tp.Advance(1);
                int delim_n = 0;
                while (_tp.Peek(delim_n) != ')') {
                    delim_n += 1;
                }
                text_delimiter = _tp.GetString(delim_n);
                _tp.Advance(delim_n + 1);
            } else {
                delimiter = _tp.Peek(0);
                _tp.Advance(1);
            }
            int start = _tp.CurrentPosition();
            int end = start;
            bool last_n = false;
            while (true) {
                char? c = _tp.Peek(0);
                if (text_delimiter != null && _tp.Match(text_delimiter)) {
                    _tp.Advance(text_delimiter.Length);
                    break;
                } else if (c == null) {
                    throw new Exception("EOF found in string constant");
                } else if (c == delimiter) {
                    if (is_long) {
                        if (_tp.Peek(1) == '}') {
                            end = _tp.CurrentPosition() - 1;
                            _tp.Advance(2);
                            break;
                        }
                        _tp.Advance(1);
                        continue;
                    }
                    end = _tp.CurrentPosition();
                    _tp.Advance(1);
                    break;
                } else if (c == '\n' && !is_long) { throw new Exception("a line break cannot end a simple raw string"); } else if (c == '\n') { last_n = true; _tp.Advance(1); } else {
                    last_n = false;
                    _tp.Advance(1);
                }
            }
            DMToken.StringTokenInfo info = new();
            info.isLong = is_long;
            info.isRaw = true;
            info.delimiter = text_delimiter != null ? text_delimiter : delimiter?.ToString();
            AcceptToken(DMToken.Kind.String, end - (last_n ? 1 : 0));
        }

        public void LexString(bool isLong) {
            char? long_delim = null;
            char? delimiter;
            if (isLong) { long_delim = '}'; delimiter = _tp.Peek(1); _tp.Advance(2); } else { delimiter = _tp.Peek(0); _tp.Advance(1); }
            List<DMToken> nestedTokens = new();
            int full_start = _tp.CurrentPosition();
            int start = _tp.CurrentPosition();
            int end;
            while (true) {
                char? c = _tp.Peek(0);
                if (c == '[') {
                    end = _tp.CurrentPosition();
                    AcceptToken(DMToken.Kind.String, end - start);
                    _tp.Advance(1);
                    LexNestedExpression();
                    AcceptToken(DMToken.Kind.String, 0);
                    start = _tp.CurrentPosition();
                } else if (c == null) {
                    throw new Exception("EOF in string");
                } else if (c == '\\' && _tp.Peek(1) == '\n') {
                    end = _tp.CurrentPosition();
                    AcceptToken(DMToken.Kind.String, end - start);
                    _tp.Advance(2); Splice();
                    start = _tp.CurrentPosition();
                } else if (c == '\n' && !isLong) {
                    throw new Exception("Newline in string");
                } else if (c == '\\') {
                    if (_tp.Match("ref[", 1)) {
                        end = _tp.CurrentPosition();
                        AcceptToken(DMToken.Kind.String, end - start);
                        _tp.Advance(5);
                        LexNestedExpression();
                        AcceptToken(DMToken.Kind.String, 0);
                        start = _tp.CurrentPosition();
                    } else {
                        _tp.Advance(2);
                    }
                } else if (long_delim != null && _tp.Peek(0) == delimiter && _tp.Peek(1) == long_delim) {
                    end = _tp.CurrentPosition();
                    _tp.Advance(2);
                    break;
                } else if (long_delim == null && c == delimiter) {
                    end = _tp.CurrentPosition();
                    _tp.Advance(1);
                    break;
                } else { _tp.Advance(1); }
            }
            DMToken.StringTokenInfo info = new();
            info.isLong = isLong;
            info.isRaw = false;
            info.delimiter = (long_delim != null ? long_delim.ToString() : "") + (delimiter != null ? delimiter.ToString() : "");
            if (nestedTokens.Count > 0) {
                AcceptToken(DMToken.Kind.String, end - start);
                bool is_interp = false;
                foreach (var token in nestedTokens) {
                    if (token.Value != null) { is_interp = true; }
                }
                if (is_interp) {
                    info.nestedTokenInfo = new DMToken.NestedTokens("root", nestedTokens);
                } else {
                    System.Text.StringBuilder sb = new();
                    foreach (var token in nestedTokens) {
                        sb.Append(token.Text);
                    }
                    //return new DMToken(DMToken.Kind.String, sb.ToString(), info, loc: current_location);
                }
            }
            //return new DMToken(DMToken.Kind.String, _tp.GetString(full_start, end), info, loc: current_location);

            void LexNestedExpression() {
                Advance();
                int bracketNesting = 0;
                
                while (!(bracketNesting == 0 && pending_tokens.Peek().IsSymbol("]")) && _tp.Peek(0) != null) {
                    if (pending_tokens.Peek().IsSymbol("[")) bracketNesting++;
                    if (pending_tokens.Peek().IsSymbol("]")) bracketNesting--;
                    Advance();
                }
                if (!pending_tokens.Peek().IsSymbol("]")) throw new Exception("Expected ']' to end expression");
            }

        }
    }
}
