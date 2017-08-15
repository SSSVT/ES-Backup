using ESBackupAndReplication.Access.Interfaces;
using ESBackupAndReplication.FileManagers;
using ESBackupAndReplication.FileManagers.Interfaces;
using ESBackupAndReplication.Objects;
using ESBackupAndReplication.Restore.Interfaces;
using System.Collections.Generic;
using System.Linq;
using ESBackupAndReplicationLibrary.Properties;

namespace ESBackupAndReplication.Restore
{
    public class RestoreManager : IRestoreManager
    {
        #region Constructors
        public RestoreManager(IAccess access)
        {
            this.Access = access;

            this.Files = new FileManager(access, this.LogFile);
            this.CompressedFiles = new CompressedFileManager(access, this.LogFile, this.Structure);
        }
        #endregion

        #region Properties
        public IAccess Access { get; }

        internal IFileManager Files { get; }
        internal IFileManager CompressedFiles { get; }

        protected string LogFile { get; } = Resources.DeletedFilesInfo;
        protected string Structure { get; } = Resources.FileStructureInfo;
        #endregion

        #region IRestoreManager
        public void Restore(List<BackupHistory> backups, string destination)
        {
            System.IO.Directory.CreateDirectory(destination);

            List<FileHistory> fileHistory = this.GetFilesHistory(backups);
            List<FileHistory> destinationFiles = this.Files.SearchForFiles(destination);
            List<FileHistory> compared = this.Files.CompareVersions(fileHistory, destinationFiles).NewOrChanged;

            foreach (var groupedFiles in compared.GroupBy(x => x.Compressed))
            {
                if (groupedFiles.Key)
                    this.CompressedFiles.RestoreFiles(groupedFiles.ToList(), destination);
                else
                    this.Files.RestoreFiles(groupedFiles.ToList(), destination);                
            }
        }
        #endregion

        #region Virtual
        protected virtual List<FileHistory> GetFilesHistory(List<BackupHistory> backup)
        {
            List<FileHistory> history = new List<FileHistory>();
            foreach (var backups in backup.GroupBy(x => x.Compressed))
            {
                if (backups.Key)
                    history.AddRange(this.CompressedFiles.GetFilesHistory(backups.ToList()));
                else
                    history.AddRange(this.Files.GetFilesHistory(backups.ToList()));
            }

            return this.Files.FilterFilesHistory(history);
        }
        #endregion
    }
}
