
namespace ClopenDream {

    public class CombinedSource {
        // state
        SourceText current_source = null;
        Stack<SourceText> _sourceTexts = new();
        Stack<SourceLocation> _sourceLocations = new();

        // saved state
        Stack<int> saved_positions = new();

        // optimizations
        private bool _is_end;
        int _cpos = 0;
        int _cLine = 0;
        int _cColumn = 0;
        char[] _ctext = null;

        public bool IsEnd { get { return _is_end; } }

        public int CurrentPosition() {
            return _cpos;
        }

        public string GetString(int start, int end) {
            return new string(_ctext, start, end - start);
        }
        public string GetString(int size) {
            int start = _cpos;
            return GetString(start, start + size);
        }
        public SourceLocation CurrentLocation() {
            var loc = new SourceLocation();
            loc.Source = current_source;
            loc.Position = _cpos;
            loc.Line = _cLine;
            loc.Column = _cColumn;
            return loc;
        }

        public char? Peek(int n) {
            if (_cpos + n < 0) { return null; }
            if (_cpos + n >= _ctext.Length) {
                return null;
            }
            return _ctext[_cpos + n];
        }
        public void Include(SourceText srctext) {
            srctext.LoadSource();
            if (saved_positions.Count != 0) { throw new Exception("attempt to SavePosition past #include boundary"); }
            if (srctext.Length == 0) { return; }
            if (current_source != null) {
                _sourceTexts.Push(current_source);
                _sourceLocations.Push(CurrentLocation());
            }
            current_source = srctext;
            //Console.WriteLine("push to " + current_source.FullPath);
            _ctext = srctext.Text;
            _cpos = 0;
            _cLine = 1;
            _cColumn = 0;
            Update();
        }
        public void SavePosition() {
            saved_positions.Push(_cpos);
        }
        public void AcceptPosition() {
            saved_positions.Pop();
            Update();
        }
        public void RestorePosition() {
            _cpos = saved_positions.Pop();
            Update();
        }
        public int SourceRemaining() {
            return _ctext.Length - _cpos;
        }

        public void Advance(int n) {
            for (int i = 0; i < n; i++) {
                ProducerNext();
            }
        }
        public char? ProducerNext() {
            if (_cpos < _ctext.Length) {
                char c = _ctext[_cpos++];
                _cColumn++;
                if (c == '\n') {
                    _cColumn = 0;
                    _cLine += 1;
                }
                return c;
            } else {
                if (_sourceTexts.Count == 0) {
                    if (current_source == null) {
                        throw new Exception("attempt to read past eof");
                    }
                    current_source = null;
                    return null;
                }
                if (saved_positions.Count > 0) {
                    throw new Exception("EndOfFile reached with saved position");
                }
                current_source = _sourceTexts.Pop();
                SourceLocation loc = _sourceLocations.Pop();
                //Console.WriteLine("pop to " + current_source.FullPath);
                _ctext = current_source.Text;
                _cpos = loc.Position;
                _cLine = loc.Line;
                _cColumn = loc.Column;
                Update();
                return null;
            }
        }

        public void Update() {
            if (_cpos + 1 == _ctext.Length && _sourceTexts.Count == 0) { _is_end = true; }
        }
        public bool ProducerEnd() {
            return _is_end;
        }

        public bool Match(string s, int offset = 0) {
            if (s.Length + offset > SourceRemaining()) {
                return false;
            }
            for (int i = 0; i < s.Length; i++) {
                if (_ctext[_cpos + i + offset] != s[i]) { return false; }
            }
            return true;
        }
    }
}