
using System;
using System.Text;
using System.Collections.Generic;

namespace DMTreeParse {

    public partial class Parser {
        private bool is_alpha(char c) {
            if (c >= 65 && c <= 90) { return true; }
            if (c >= 97 && c <= 122) { return true; }
            return false;
        }
        private bool is_digit(char c) {
            if (c >= 48 && c <= 57) { return true; }
            return false;
        }
        private bool is_alphanum(char c) {
            if (is_alpha(c)) { return true; }
            if (is_digit(c)) { return true; }
            return false;
        }
        private bool is_ident(char c) {
            if (is_alphanum(c)) { return true; }
            if (c == '_') { return true; }
            return false;
        }
        private bool is_ident_start(char c) {
            if (is_alpha(c)) { return true; }
            if (c == '_') { return true; }
            return false;
        }
        private bool is_deref_start(char c) {
            if (is_alpha(c)) { return true; }
            if (c == '<') { return true; }
            if (c == '_') { return true; }
            return false;
        }
        private bool is_path_separator(char c) {
            if (c == '/') { return true; }
            if (c == '.') { return true; }
            if (c == ':') { return true; }
            return false;
        }
        private bool is_deref(char c) {
            if (c == '.') { return true; }
            if (c == ':') { return true; }
            return false;
        }

        private bool is_delimiter(char c) {
            if (c == '\'') { return true; }
            if (c == '"') { return true; }
            return false;
        }
        private bool is_space(char c) {
            return c == 32;
        }
        public void skipws() {
            while (is_space(getc())) {
                _cPos += 1;
            }
        }
        public RET reset<RET>(Func<RET> fn) {
            var start = _cPos;
            RET ret = fn();
            _cPos = start;
            return ret;
        }
        public bool match(string main, int start, string substr) {
            if (left() < substr.Length) { return false; }
            var end = start + substr.Length;
            var subi = 0;
            for (var i = start; i < end; i++, subi++) {
                if (main[i] != substr[subi]) { return false; }
            }
            return true;
        }

        public string read_until(char mark) {
            var start = _cPos;
            while (getc() != mark) {
                _cPos += 1;
            }
            return _cText.Substring(start, _cPos - start);
        }
        public string read_ident() {
            var start = _cPos;
            if (match(_cText, _cPos, "<expression>")) {
                _cPos += "<expression>".Length;
                return "<expression>";
            }
            while (is_ident(getc())) {
                _cPos += 1;
            }
            return _cText.Substring(start, _cPos - start);
        }

        public string read_alphanum() {
            var start = _cPos;
            while (is_alphanum(getc())) {
                _cPos += 1;
            }
            return _cText.Substring(start, _cPos - start);
        }

        public string read_exponent() {
            var start = _cPos;
            if (match(_cText, _cPos, "e+")) {
                _cPos += 2;
                while (is_digit(getc())) {
                    _cPos += 1;
                }
                return _cText.Substring(start, _cPos - start);
            }
            else if (match(_cText, _cPos, "e-")) {
                _cPos += 2;
                while (is_digit(getc())) {
                    _cPos += 1;
                }
                return _cText.Substring(start, _cPos - start);
            }
            return null;
        }

        public string read_decimal() {
            var start = _cPos;
            if (getc() == '.') {
                _cPos += 1;
                while (is_digit(getc())) {
                    _cPos += 1;
                }
                return _cText.Substring(start, _cPos - start);
            }
            return null;
        }
        public string read_numeric() {
            var start = _cPos;
            string whole = "";
            string part = "";
            string expon = "";
            if (getc() == '-') { _cPos += 1; }
            while (is_digit(getc())) {
                _cPos += 1;
            }
            whole = _cText.Substring(start, _cPos - start);
            var decim = read_decimal();
            if (decim != null) {
                part = decim;
            }
            var expo = read_exponent();
            if (expo != null) {
                expon = expo;
            }
            return whole + part + expon;

        }
        public string read_string() {
            StringBuilder sb = new();
            if (getc() != '\"') { return null; }
            _cPos += 1;
            var true_end = "\") ";
            var reading_string = true;
            while (reading_string) {
                if (left() == 0) {
                    sb.Append('\n');
                    NextLine();
                    continue;
                }
                if (match(_cText, _cPos, true_end)) {
                    var delim_start = _cPos;
                    _cPos += 3;
                    if (ReadLocation() != null) {
                        reading_string = false;
                        _cPos = delim_start + 1;
                        continue;
                    }
                    else {
                        _cPos = delim_start;
                    }
                }
                if (left() >= 2 && getc() == '\\' && getc(1) == '\\') { sb.Append(getc()); sb.Append(getc(1)); _cPos += 2; continue; }
                if (left() >= 2 && getc() == '\\' && getc(1) == '"') { sb.Append(getc()); sb.Append(getc(1)); _cPos += 2; continue; }
                sb.Append(getc()); 
                _cPos += 1;
            }
            return sb.ToString();
        }

        public string read_resource() {
            StringBuilder sb = new();
            if (getc() != '\'') { return null; }
            _cPos++;
            var reading_string = true;
            while (reading_string) {
                if (getc() == '\'') { _cPos++;  break; }
                sb.Append(getc());
                _cPos++;
            }
            return sb.ToString();
        }
        public string read_path_segment() {
            var start = _cPos;
            if (is_path_separator(getc())) { _cPos++; }
            if (match(_cText, _cPos, "<expression>")) {
                _cPos += "<expression>".Length;
                return _cText.Substring(start, _cPos - start);
            }
            else {
                if (!is_ident_start(getc())) { return null; }
                while (is_ident(getc())) {
                    _cPos += 1;
                }
                return _cText.Substring(start, _cPos - start);
            }
        }
        public string[] read_path() {
            List<string> segments = new();
            bool reading_segments = true;
            int start;

            while (reading_segments) {
                start = _cPos;
                string segment = read_path_segment();
                if (segment == null) {
                    reading_segments = false;
                    _cPos = start;
                }
                else {
                    segments.Add(segment);
                    if (is_path_separator(getc())) { segments.Add(getc().ToString()); _cPos++; }
                    else { reading_segments = false; }
                }
            }

            if (getc() == '/') { segments.Add(getc().ToString()); _cPos++; }
            return segments.ToArray();
        }

        public string read_parens() {
            if (getc() != '(') { return null; }
            _cPos += 1;
            var start = _cPos;
            while (getc() != ')') { _cPos++; }
            var parenstr = _cText.Substring(start, _cPos - start);
            _cPos += 1;
            return parenstr;
        }

        public bool has_parens() {
            if (getc() != '(') { return false; }
            return true;
        }

        public T read_parens<T>(Func<T> inner) {
            if (getc() != '(') { throw new Exception("expected ("); }
            _cPos += 1;
            T v = inner();
            if (getc() != ')') { throw new Exception("expected )"); }
            _cPos += 1;
            return v;
        }
    }
}