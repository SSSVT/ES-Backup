using ESBackupAndReplication.Objects;
using System.Collections.Generic;

namespace ESBackupAndReplication.Backup.Interfaces
{
    interface IBackupManager
    {
        void FullBackup(string source, string destination, string searchFilePattern = ".*", string searchDirectoryPattern = ".*", bool compression = false);
        void DifferentialBackup(string source, string destination, BackupHistory baseBackup, string searchFilePattern = ".*", string searchDirectoryPattern = ".*", bool compression = false);
        void IncrementalBackup(string source, string destination, List<BackupHistory> previousBackups, string searchFilePattern = ".*", string searchDirectoryPattern = ".*", bool compression = false);
    }
}