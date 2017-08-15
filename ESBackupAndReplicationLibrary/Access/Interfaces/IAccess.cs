using ESBackupAndReplication.Objects;

namespace ESBackupAndReplication.Access.Interfaces
{
    public interface IAccess
    {
        Directory ListDirectory(string path);

        void CreateDirectory(string path);
        void DeleteDirectory(string path);
        void EnsureDirectoryExists(string path);
        void EnsureDirectoryEmpty(string path);

        void CreateFile(string path);
        void DeleteFile(string path);
        void CopyFile(string source, string destination, bool overwrite = false);
        void RestoreFile(string source, string destination, bool overwrite = false);

        void WriteToFile(string path, string text);
        void AppendToFile(string path, string text);

        string ReadFile(string path);

        bool FileExists(string path);
        bool DirectoryExists(string path);
    }
}
