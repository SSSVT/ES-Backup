using System.Collections.Generic;

namespace ESBackupAndReplication.Objects
{
    public class ComparedHistory
    {
        #region Constructors
        public ComparedHistory(List<FileHistory> changed = null, List<FileHistory> toDelete = null)
        {
            this.NewOrChanged = changed ?? new List<FileHistory>();
            this.Deleted = toDelete ?? new List<FileHistory>();
        }
        #endregion

        #region Properties
        public List<FileHistory> Deleted { get; }
        public List<FileHistory> NewOrChanged { get; }
        #endregion
    }
}
