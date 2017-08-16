using System.IO;
using System.Linq;

namespace ESBackupAndReplication.Helpers
{
    public static class PathHelper
    {
        public static string NewLine { get; } = "\n";

        public static char[] DirectorySeparators { get; } = GetDirectorySeparators();
        private static char[] GetDirectorySeparators()
        {
            return new char[] { '/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }.Distinct().ToArray();
        }

        public static string GetRelativePath(string root, string absolute)
        {
            root = CorrectDirectorySeparators(root);
            absolute = CorrectDirectorySeparators(absolute);

            root = root.TrimEnd(DirectorySeparators);
            int index = absolute.IndexOf(root);
            
            string relative = string.Join("", absolute.Skip(index + root.Length)).TrimStart(DirectorySeparators);

            return relative;
        }

        public static string GetParent(string path)
        {
            path = CorrectDirectorySeparators(path);

            int index = path.LastIndexOf('/');

            return path.Substring(0, index);
        }

        public static string CorrectDirectorySeparators(string path, char directorySeparatorChar = '/')
        {
            foreach (char separator in DirectorySeparators)
                path = path.Replace(separator, directorySeparatorChar);

            return path;
        }
    }
}
