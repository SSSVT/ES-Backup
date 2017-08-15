using ESBackupAndReplication.Access.Interfaces;
using ESBackupAndReplication.Objects;

namespace ESBackupAndReplication.Access
{
    public abstract class AbAccess : IAccess
    {
        public void EnsureDirectoryExists(string path)
        {
            try
            {
                this.CreateDirectory(path);
            }
            catch (DirectoryAlreadyExistsException)
            {
                //Ok
            }
        }

        public void EnsureDirectoryEmpty(string path)
        {
            Directory directory = this.ListDirectory(path);

            foreach (string dir in directory.Directories)
                this.DeleteDirectory(dir);
            foreach (FileHistory file in directory.Files)
                this.DeleteFile(file.Path);
        }

        public abstract Directory ListDirectory(string path);

        public abstract void CreateDirectory(string path);
        public abstract void DeleteDirectory(string path);

        public abstract void CreateFile(string path);
        public abstract void DeleteFile(string path);
        public abstract void CopyFile(string source, string destination, bool overwrite = false);
        public abstract void RestoreFile(string source, string destination, bool overwrite = false);

        public abstract void WriteToFile(string path, string text);
        public abstract void AppendToFile(string path, string text);
        public abstract string ReadFile(string path);

        public abstract bool FileExists(string path);
        public abstract bool DirectoryExists(string path);
    }
}
