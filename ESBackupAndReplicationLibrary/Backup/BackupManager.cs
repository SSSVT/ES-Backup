using ESBackupAndReplication.Access.Interfaces;
using ESBackupAndReplication.Backup.Interfaces;
using ESBackupAndReplication.FileManagers;
using ESBackupAndReplication.FileManagers.Interfaces;
using ESBackupAndReplication.Helpers;
using ESBackupAndReplication.Objects;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ESBackupAndReplicationLibrary.Properties;

namespace ESBackupAndReplication.Backup
{
    public class BackupManager : IBackupManager, IDeleteBackup
    {
        #region Constructors
        public BackupManager(IAccess access)
        {
            this.Access = access;

            Files = new FileManager(access, LogFile);
            CompressedFiles = new CompressedFileManager(access, LogFile, Structure);
        }
        #endregion

        #region Properties
        public IAccess Access { get; }
        internal IFileManager Files { get; }
        internal IFileManager CompressedFiles { get; }

        protected string LogFile { get; } = Resources.DeletedFilesInfo;
        protected string Structure { get; } = Resources.FileStructureInfo;

        protected List<FileHistory> _SourceFiles { get; set; }
        protected List<FileHistory> _BackupFiles { get; set; }
        protected List<FileHistory> _FilesToBackup { get; set; }
        #endregion

        #region IBackupManager
        public virtual void DifferentialBackup(string source, string destination, BackupHistory baseBackup, string searchFilePattern = ".*", string searchDirectoryPattern = ".*", bool compression = false)
        {
            this.IncrementalBackup(source, destination, new List<BackupHistory>() { baseBackup }, searchFilePattern, searchDirectoryPattern, compression);
        }

        public virtual void FullBackup(string source, string destination, string searchFilePattern = ".*", string searchDirectoryPattern = ".*", bool compression = false)
        {
            if(!compression)
                Access.EnsureDirectoryExists(destination);

            this._SourceFiles = this.Files.SearchForFiles(source, searchFilePattern, searchDirectoryPattern);

            if (compression)
                this.CompressedFiles.CopyFiles(this._SourceFiles, destination);
            else
                this.Files.CopyFiles(this._SourceFiles, destination);
        }

        public virtual void IncrementalBackup(string source, string destination, List<BackupHistory> previousBackups, string searchFilePattern = ".*", string searchDirectoryPattern = ".*", bool compression = false)
        {
            if (!compression)
                Access.EnsureDirectoryExists(destination);

            this._SourceFiles = this.Files.SearchForFiles(source, searchFilePattern, searchDirectoryPattern); //original files
            this._BackupFiles = this.GetFilesHistory(previousBackups); //all changes

            ComparedHistory compared = this.Files.CompareVersions(this._SourceFiles, this._BackupFiles); //new or changed files

            if (compression)
                this.CompressedFiles.CopyFiles(compared.NewOrChanged, destination);
            else
                this.Files.CopyFiles(compared.NewOrChanged, destination);

            this.Files.WriteInLog(destination, compared.Deleted);
        }
        #endregion

        #region Virtual
        protected virtual List<FileHistory> GetFilesHistory(List<BackupHistory> backup)
        {
            List<FileHistory> history = new List<FileHistory>();
            foreach(var backups in backup.GroupBy(x => x.Compressed))
            {
                if (backups.Key)
                    history.AddRange(this.CompressedFiles.GetFilesHistory(backups.ToList()));
                else
                    history.AddRange(this.Files.GetFilesHistory(backups.ToList()));
            }

            return this.Files.FilterFilesHistory(history);
        }

        protected virtual void DeleteFilesFromDestination(List<FileHistory> files, string source, string destination)
        {
            DirectoryInfo sourceDir = new DirectoryInfo(source);
            foreach (FileHistory item in files)
            {
                FileInfo file = new FileInfo(item.Path);
                string relativePath = PathHelper.GetRelativePath(sourceDir.FullName, file.FullName);
                string absolutePath = Path.Combine(destination, relativePath);

                Access.DeleteFile(absolutePath);
            }
        }

        protected virtual void DeleteFiles(List<FileHistory> files)
        {
            foreach (FileHistory item in files)
            {
                Access.DeleteFile(item.Path);
            }
        }
        #endregion

        #region IDeleteBackup
        public void Delete(List<BackupHistory> list)
        {
            foreach (BackupHistory item in list)
            {
                this.Delete(item);
            }
        }

        public void Delete(BackupHistory item)
        {
            Access.DeleteDirectory(item.Destination);
        }
        #endregion
    }
}