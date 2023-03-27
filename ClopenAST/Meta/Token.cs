
using OpenDreamShared.Compiler;

namespace ClopenDream {
    public partial class DMToken {
        public enum Kind : byte {
            Info,
            EndOfFile,
            Whitespace,
            Newline,
            Symbol,
            String,
            Identifier,
            Numeric,
        }

        public Kind K;
        public object Value;
        public string Text;
        public SourceSpan? Span;

        public DMToken(DMToken.Kind ty, object value, SourceSpan? span = null) {
            K = ty;
            Value = value;
            Span = span;
        }
        public override string ToString() {
            return $"{K} {Value}";
        }
        public bool IsSymbol(string sym) {
            if (K != Kind.Symbol) { return false; }
            if (Text != sym) { return false; }
            return true;
        }
        public bool IsIdentifier(string id) {
            if (K != Kind.Identifier) { return false; }
            if (Text != id) { return false; }
            return true;
        }
        public bool CheckText(Kind ty, string s) {
            if (K == ty && Text == s) {
                return true;
            }
            return false;
        }
        public static string PrintTokens(IEnumerable<DMToken> tokens) {
            StringBuilder sb = new();
            foreach (var token in tokens) {
                sb.Append(token.ToString() + " | ");
            }
            sb.Append("");
            return sb.ToString();
        }

        public class NestedTokens {
            public string Type;
            public List<DMToken> Tokens;

            public NestedTokens(string type, List<DMToken> tokens) {
                Type = type;
                Tokens = tokens;
            }

            public NestedTokens Copy() {
                return new NestedTokens(Type, Tokens);
            }

            public void Transform(Func<string, string> fn) {
                foreach (var token in Tokens) {
                    if (token.K == Kind.String) {
                        token.Text = fn(token.Text);
                    }
                    if (token.Value is StringTokenInfo info && info.nestedTokenInfo != null) {
                        info.Transform(fn);
                    }
                    if (token.Value is NestedTokens nti && nti.Tokens != null) {
                        nti.Transform(fn);
                    }
                }
            }
        }
        public class StringTokenInfo {
            public string delimiter;
            public bool isRaw;
            public bool isLong;
            public NestedTokens nestedTokenInfo;

            public StringTokenInfo Copy() {
                var sti = new StringTokenInfo();
                sti.delimiter = delimiter;
                sti.isRaw = isRaw;
                sti.isLong = isLong;
                sti.nestedTokenInfo = nestedTokenInfo.Copy();
                return sti;
            }

            public void Transform(Func<string, string> fn) {
                nestedTokenInfo?.Transform(fn);
            }
        }
    }
}