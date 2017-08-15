using System;
using System.Collections.Generic;
using System.Linq;
using ESBackupAndReplication.Objects;
using System.IO;
using System.Text.RegularExpressions;
using ESBackupAndReplication.Access.Interfaces;
using ESBackupAndReplication.FileManagers.Interfaces;
using ESBackupAndReplication.Helpers;
using System.Text;

namespace ESBackupAndReplication.FileManagers
{
    internal abstract class AbFileManager : IFileManager
    {
        #region Constructors
        public AbFileManager(IAccess access, string logFile)
        {
            this.Access = access;
            this.LogFile = logFile;
        }
        #endregion

        #region Properties
        public IAccess Access { get; }
        public string LogFile { get; }
        #endregion

        #region IFileManagerAbstract
        public abstract void CopyFiles(List<FileHistory> files, string destination);
        public abstract void RestoreFiles(List<FileHistory> files, string destination);
        public abstract List<FileHistory> SearchForBackupedFiles(string path, string searchFilePattern = ".*", string searchDirectoryPattern = ".*");
        #endregion

        #region Virtual
        protected virtual string GetLogPath(string destination)
        {
            return Path.Combine(destination, this.LogFile);
        }

        protected virtual bool IsCompressed()
        {
            return false;
        }
        #endregion

        #region IFileManager
        public List<FileHistory> GetFilesHistory(List<BackupHistory> previousBackups)
        {
            List<FileHistory> list = new List<FileHistory>();
            foreach (BackupHistory item in previousBackups.OrderBy(x => x.UTCStart))
            {
                List<FileHistory> backupedFiles = this.SearchForBackupedFiles(item.Destination);
                list = this.FilterFilesHistory(backupedFiles, true);

                foreach (FileHistory log in this.GetDeletedFilesFromLog(item.Destination))
                {
                    list.Remove(list.Where(x => x.RelativePath == log.RelativePath).FirstOrDefault());
                }
            }

            list.Remove(list.FirstOrDefault(x => x.RelativePath == this.LogFile)); //Remove log file from files
            return list;
        }

        public List<FileHistory> FilterFilesHistory(List<FileHistory> toFilter, bool setCompressed = false)
        {
            List<FileHistory> list = new List<FileHistory>();
            foreach (FileHistory file in toFilter)
            {
                FileHistory tmp = list.Where(x => x.RelativePath == file.RelativePath).FirstOrDefault();
                if (tmp == null)
                {
                    list.Add(file);

                    if (setCompressed)
                        file.Compressed = this.IsCompressed();
                }
                else
                {
                    tmp.TimeStamp = file.TimeStamp;
                    tmp.Compressed = file.Compressed;
                    tmp.UpdateRoot(file.Root);
                }
            }

            return list;
        }

        public ComparedHistory CompareVersions(List<FileHistory> source, List<FileHistory> history)
        {
            ComparedHistory compared = new ComparedHistory();

            foreach (FileHistory src in source) //new or modified files
            {
                FileHistory hst = history.Where(x => String.Equals(x.RelativePath, src.RelativePath, StringComparison.InvariantCulture)).FirstOrDefault();

                if (hst == null || (src.TimeStamp - hst.TimeStamp).TotalSeconds > 1)
                    compared.NewOrChanged.Add(src);
            }
            foreach (FileHistory hst in history) //find deleted files
            {
                FileHistory org = source.Where(x => String.Equals(x.RelativePath, hst.RelativePath, StringComparison.InvariantCulture)).FirstOrDefault();
                if (org == null)
                    compared.Deleted.Add(hst);
            }

            return compared;
        }

        public List<FileHistory> SearchForFiles(string path, string searchFilePattern, string searchDirectoryPattern)
        {
            List<FileHistory> list = this.SearchForFilesRecursively(path, searchFilePattern, searchDirectoryPattern);
            foreach (FileHistory file in list)
                file.Root = path;

            return list;
        }


        public void WriteInLog(string path, List<FileHistory> files)
        {
            StringBuilder sb = new StringBuilder();
            foreach (FileHistory item in files)
            {
                sb.Append(item.Serialize() + PathHelper.NewLine);
            }

            this.Access.WriteToFile(this.GetLogPath(path), sb.ToString());
        }

        public List<FileHistory> GetDeletedFilesFromLog(string path)
        {
            List<FileHistory> list = new List<FileHistory>();
            try
            {
                string[] read = this.Access.ReadFile(this.GetLogPath(path)).Split(new string[] { PathHelper.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in read)
                {
                    list.Add(FileHistory.Deserialize(line, path));
                }
            }
            catch (FileNotFoundException)
            {
                //ok, log does not exist, assuming no deleted files
            }

            return list;
        }
        #endregion

        #region Protected
        protected virtual List<FileHistory> SearchForFilesRecursively(string path, string searchFilePattern, string searchDirectoryPattern)
        {
            List<FileHistory> list = new List<FileHistory>();
            DirectoryInfo dir = new DirectoryInfo(path);
            foreach (FileInfo item in dir.GetFiles().Where(x => Regex.IsMatch(x.Name, searchFilePattern)))
            {
                list.Add(new FileHistory()
                {
                    Path = item.FullName,
                    TimeStamp = item.LastWriteTimeUtc,
                    Root = path
                });
            }
            foreach (DirectoryInfo item in dir.GetDirectories().Where(x => Regex.IsMatch(x.Name, searchDirectoryPattern)))
            {
                foreach (FileHistory hst in this.SearchForFilesRecursively(item.FullName, searchFilePattern, searchDirectoryPattern))
                {
                    list.Add(hst);
                }
            }
            return list;
        }
        #endregion
    }
}
