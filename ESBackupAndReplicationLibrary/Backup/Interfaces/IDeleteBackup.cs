using ESBackupAndReplication.Objects;
using System.Collections.Generic;

namespace ESBackupAndReplication.Backup.Interfaces
{
    public interface IDeleteBackup
    {
        void Delete(List<BackupHistory> list);
        void Delete(BackupHistory item);
    }
}
