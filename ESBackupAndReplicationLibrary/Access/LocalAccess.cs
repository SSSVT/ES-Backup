using ESBackupAndReplication.Access.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using ESBackupAndReplication.Objects;
using IO = System.IO;

namespace ESBackupAndReplication.Access
{
    public class LocalAccess : AbAccess, IAccess
    {
        #region IAccess
        public override void CreateDirectory(string path)
        {
            if (IO.Directory.Exists(path))
                throw new DirectoryAlreadyExistsException(path);

            try
            {
                IO.Directory.CreateDirectory(path);
            }
            catch (IO.DirectoryNotFoundException ex)
            {
                throw new DirectoryNotFoundException(path, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new PermissionsException(path, ex);
            }
        }

        public override void DeleteDirectory(string path)
        {
            try
            {
                IO.Directory.Delete(path, true);
            }
            catch (IO.DirectoryNotFoundException ex)
            {
                throw new DirectoryNotFoundException(path, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new PermissionsException(path, ex);
            }
        }

        public override void CreateFile(string path)
        {
            if (IO.File.Exists(path))
                throw new FileAlreadyExistsException(path);

            try
            {
                IO.File.Create(path).Close();
            }
            catch (IO.DirectoryNotFoundException ex)
            {
                throw new DirectoryNotFoundException(path, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new PermissionsException(path, ex);
            }
        }

        public override void DeleteFile(string path)
        {
            if(!IO.File.Exists(path))
                throw new FileNotFoundException(path);

            try
            {
                IO.File.Delete(path);
            }
            catch (IO.DirectoryNotFoundException ex)
            {
                throw new DirectoryNotFoundException(path, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new PermissionsException(path, ex);
            }
        }

        public override string ReadFile(string path)
        {
            try
            {
                return IO.File.ReadAllText(path);
            }
            catch (IO.DirectoryNotFoundException ex)
            {
                throw new DirectoryNotFoundException(path, ex);
            }
            catch (IO.FileNotFoundException ex)
            {
                throw new FileNotFoundException(path, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new PermissionsException(path, ex);
            }
        }

        public override void WriteToFile(string path, string text)
        {
            try
            {
                IO.File.WriteAllText(path, text);
            }
            catch (IO.DirectoryNotFoundException ex)
            {
                throw new DirectoryNotFoundException(path, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new PermissionsException(path, ex);
            }
        }

        public override void AppendToFile(string path, string text)
        {
            try
            {
                IO.File.AppendAllText(path, text);
            }
            catch (IO.DirectoryNotFoundException ex)
            {
                throw new DirectoryNotFoundException(path, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new PermissionsException(path, ex);
            }
        }

        public override void CopyFile(string source, string destination, bool overwrite)
        {
            this.EnsureDirectoryExists(new IO.FileInfo(destination).Directory.FullName);

            try
            {
                IO.File.Copy(source, destination, overwrite);
            }
            catch (IO.DirectoryNotFoundException ex)
            {
                throw new DirectoryNotFoundException(destination, ex);
            }
            catch (IO.FileNotFoundException ex)
            {
                throw new FileNotFoundException(destination, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new PermissionsException(destination, ex);
            }
            catch (IO.IOException ex)
            {
                throw new FileAlreadyExistsException(destination, ex);
            }
        }

        public override void RestoreFile(string source, string destination, bool overwrite = false)
        {
            this.CopyFile(source, destination, overwrite);
        }

        public override Directory ListDirectory(string path)
        {
            try
            {
                IO.DirectoryInfo directoryInfo = new IO.DirectoryInfo(path);
                List<string> directories = IO.Directory.GetDirectories(path).ToList();
                List<FileHistory> files = directoryInfo.GetFiles()
                    .Select(x =>
                        new FileHistory()
                        {
                            Path = x.FullName,
                            TimeStamp = x.LastWriteTimeUtc,
                        }).ToList();

                return new Directory(path, directories, files);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new PermissionsException(path, ex);
            }
            catch (IO.DirectoryNotFoundException ex)
            {
                throw new DirectoryNotFoundException(path, ex);
            }
        }

        public override bool DirectoryExists(string path)
        {
            return IO.Directory.Exists(path);
        }

        public override bool FileExists(string path)
        {
            return IO.File.Exists(path);
        }
        #endregion
    }
}
