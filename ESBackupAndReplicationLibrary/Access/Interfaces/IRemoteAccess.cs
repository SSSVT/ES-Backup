namespace ESBackupAndReplication.Access.Interfaces
{
    public interface IRemoteAccess : IAccess
    {
        bool Connected { get; }

        void Connect();
        void Disconnect();
    }
}
