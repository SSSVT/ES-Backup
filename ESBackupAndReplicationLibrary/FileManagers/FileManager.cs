using ESBackupAndReplication.Access.Interfaces;
using ESBackupAndReplication.Objects;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ESBackupAndReplication.FileManagers
{
    internal class FileManager : AbFileManager
    {
        #region Constructors
        public FileManager(IAccess access, string logFile) : base(access, logFile)
        {
        }
        #endregion

        #region IFileManager
        public override void CopyFiles(List<FileHistory> files, string destination)
        {
            foreach (FileHistory item in files)
            {
                string destinationFilePath = Path.Combine(destination, item.RelativePath);
                this.Access.CopyFile(item.Path, destinationFilePath);
            }
        }

        public override void RestoreFiles(List<FileHistory> files, string destination)
        {
            foreach (FileHistory item in files)
            {
                string destinationFilePath = Path.Combine(destination, item.RelativePath);
                this.Access.RestoreFile(item.Path, destinationFilePath, true);
            }
        }

        public override List<FileHistory> SearchForBackupedFiles(string path, string searchFilePattern = ".*", string searchDirectoryPattern = ".*")
        {
            List<FileHistory> list = this.SearchForBackupedFilesRecursively(path, searchFilePattern, searchDirectoryPattern);
            foreach (FileHistory file in list)
                file.Root = path;

            return list;
        }
        #endregion

        #region Protected
        protected virtual List<FileHistory> SearchForBackupedFilesRecursively(string path, string searchFilePattern = ".*", string searchDirectoryPattern = ".*")
        {
            List<FileHistory> list = new List<FileHistory>();
            Objects.Directory directory = this.Access.ListDirectory(path);

            foreach (string dir in directory.Directories.Where(x => Regex.IsMatch(x, searchDirectoryPattern)))
            {
                list.AddRange(this.SearchForBackupedFilesRecursively(dir, searchFilePattern, searchDirectoryPattern));
            }

            foreach (FileHistory file in directory.Files.Where(x => Regex.IsMatch(Path.GetFileName(x.Path), searchFilePattern)))
            {
                list.Add(file);
            }

            return list;
        }
        #endregion
    }
}
