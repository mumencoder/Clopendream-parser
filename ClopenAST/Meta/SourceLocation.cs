
namespace ClopenDream {
    public record struct SourceLocation {
        public SourceText Source;
        public int Position;
        public int Line;
        public int Column;

        public override string ToString() {
            return $"{Source.IncludePath}:{Line}:{Column}";
        }
    }

    public record struct SourceSpan {

        public SourceSpan(SourceLocation start, SourceLocation end) {
            Start = start;
            End = end;
        }

        public SourceLocation Start;
        public SourceLocation End;
    }
}