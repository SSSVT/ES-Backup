using ESBackupAndReplication.Objects;
using System.Collections.Generic;

namespace ESBackupAndReplication.Restore.Interfaces
{
    public interface IRestoreManager
    {
        void Restore(List<BackupHistory> backups, string destination);
    }
}
