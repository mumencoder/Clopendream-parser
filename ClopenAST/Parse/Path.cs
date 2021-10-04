using System;
using System.Collections.Generic;

namespace ClopenDream {

    public partial class Parser {
        public string[] ExpressionDereference() {
            List<string> segments = new();
            //Console.WriteLine("?" + _cText + "|||" + getc());
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
        public string ConstPath(int explicitStatus) {
            string explic = "";
            var start = _cPos;
            if (getc() == '/') {
                explic = "/";
                if (explicitStatus == -1) { return null; }
                _cPos++;
            }
            else if (getc() == ':') {
                explic = ":";
                if (explicitStatus == -1) { return null; }
                _cPos++;
            }
            else if (getc() == '.') {
                explic = ".";
                if (explicitStatus == -1) { return null; }
                _cPos++;
            }
            else if (explicitStatus == 1) { return null; }

            var ident = read_ident();
            if (ident == null && explic != "") {
                return explic;
            }
            else if (ident == null) {
                _cPos = start;
                return null;
            }
            return explic + ident;
        }

        public string[] ExpressionPath(int explicitStatus) {
            List<string> segments = new();
            var cpath = ConstPath(explicitStatus);
            segments.Add(cpath);
            while (cpath != null && is_path_separator(getc())) {
                if (getc() == '/') {
                    _cPos++;
                    cpath = ConstPath(0);
                    if (cpath == null) {
                        segments.Add("/");
                        break;
                    }
                    segments.Add("/");
                    segments.Add(cpath);
                }
                else if (getc() == ':') {
                    _cPos++;
                    cpath = ConstPath(0);
                    if (cpath == null) {
                        throw new Exception("dangling :");
                    }
                    segments.Add(":");
                    segments.Add(cpath);
                }
                else if (getc() == '.') {
                    _cPos++;
                    cpath = ConstPath(0);
                    if (cpath == null) {
                        throw new Exception("dangling .");
                    }
                    segments.Add(".");
                    segments.Add(cpath);
                }
                else {
                    return segments.ToArray();
                }
            }
            return segments.ToArray();
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
            if (deref != null) {
                return ("deref", deref);
            }
            throw new Exception();
        }
    }
}