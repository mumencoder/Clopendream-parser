
using System;

namespace ClopenDream {

    public class ByondCompileError : System.Exception {
        public string Text;
        public ByondCompileError(string t) {
            Text = t;
        }
    }
    public partial class Parser {
        public string ParseTopLevel() {
            if (_cText[0] == '\t') {
                return null;
            }
            if (_cText.StartsWith("loading")) {
                return _cText;
            }
            if (_cText.StartsWith("error: ")) {
                byond_errors.Add(new ByondCompileError(_cText));
                return _cText;
            }
            var colon1 = _cText.IndexOf(':');
            if (colon1 != -1) {
                var colon2 = _cText.IndexOf(':', colon1 + 1);
                if (colon2 != -1) {
                    var colon3 = _cText.IndexOf(':', colon2 + 1);
                    if (colon3 != -1) {
                        var warn = _cText.Substring(colon2 + 1, colon3 - colon2 - 1);
                        if (warn == "warning") {
                            return _cText;
                        }
                        if (warn == "error") {
                            byond_errors.Add( new ByondCompileError(_cText) );
                            return _cText;
                        }
                    }
                }
            }
            return null;
        }
    }
}