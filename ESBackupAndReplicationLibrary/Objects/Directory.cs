using System.Collections.Generic;

namespace ESBackupAndReplication.Objects
{
    public class Directory
    {
        #region Construcors
        public Directory(string path, List<string> directories, List<FileHistory> files)
        {
            this.Path = path;
            this.Directories = directories;
            this.Files = files;
        }
        #endregion

        #region Properties
        public string Path { get; }
        public List<string> Directories { get; }
        public List<FileHistory> Files { get; }
        #endregion
    }
}
