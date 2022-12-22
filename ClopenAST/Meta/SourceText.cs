
namespace ClopenDream {

    public class SourceText {
        public char[] Text = null;
        public string IncludeBaseDir;
        public string IncludePath;
        public string RootDir;
        public string FullPath;
        public string FileName;

        public int Length { get { return Text.Length; } }

        public SourceText(string include_base_dir, string include_path) {
            IncludeBaseDir = include_base_dir;
            IncludePath = include_path.Replace('\\', Path.DirectorySeparatorChar);
            FullPath = Path.Combine(IncludeBaseDir, IncludePath);
            RootDir = Path.GetDirectoryName(FullPath);
            FileName = Path.GetFileName(FullPath);
        }
        public void LoadSource() {
            if (Text != null) { return; }
            if (!File.Exists(FullPath)) {
                Text = "\n".ToCharArray();
                //throw new Exception("file does not exist " + FullPath);
                Console.WriteLine("file does not exist " + FullPath);
                return;
            }
            string source = File.ReadAllText(FullPath);
            source = source.Replace("\r\n", "\n");
            source += '\n';
            Text = source.ToCharArray();
        }
    }

}