
namespace ClopenDream {
    class Char {
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

    }
}