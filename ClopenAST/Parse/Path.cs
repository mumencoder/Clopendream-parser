
namespace ClopenDream {

    public partial class Parser {
        public string[] ExpressionDereference() {
            List<string> segments = new();
            do {
                if (!is_deref_start(getc())) {
                    if (segments.Count == 0) { return null; }
                    return segments.ToArray();
                }
                var ident = read_ident();
                segments.Add(ident);
                if (!is_deref(getc())) {
                    return segments.ToArray();
                }
                segments.Add(getc().ToString());
                _cPos++;
            } while (true);
        }

        public string[] ExpressionPath(int explicitStatus) {
            List<string> segments = new();
            return read_path();
        }

        (string, object) ExprParens() {
            if (match(_cText, _cPos, ".")) {
                _cPos += 1;
                return ("expr", ".");
            }
            string s = read_string();
            if (s != null) {
                return ("string", s);
            }
            string rsc = read_resource();
            if (rsc != null) {
                return ("resource", rsc);
            }
            string[] deref = ExpressionDereference();
            if (deref != null) {
                return ("deref", deref);
            }
            string numeric = read_numeric();
            if (numeric != "") {
                return ("num", numeric);
            }
            string[] path = read_path();
            if (path != null) {
                return ("path", path);
            }
            throw new Exception();
        }

        (string, object) CallParens() {
            if (match(_cText, _cPos, "..")) {
                _cPos += 2;
                return ("expr", "..");
            }
            if (match(_cText, _cPos, ".")) {
                _cPos += 1;
                return ("expr", ".");
            }
            string[] deref = ExpressionDereference();
            if (deref != null && _cText[_cPos] == ')') {
                return ("deref", deref);
            }
            string[] path = ExpressionPath(0);
            if (path != null && _cText[_cPos] == ')') {
                return ("path", path);
            }
            throw new Exception();
        }
    }
}