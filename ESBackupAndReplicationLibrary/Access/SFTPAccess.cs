using ESBackupAndReplication.Access.Interfaces;
using ESBackupAndReplication.Authentication;
using ESBackupAndReplication.Helpers;
using ESBackupAndReplication.Objects;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IO = System.IO;

namespace ESBackupAndReplication.Access
{
    public class SFTPAccess : AbAccess, IRemoteAccess
    {
        #region Constructors
        public SFTPAccess(RemoteAuthentication authentication)
        {
            this.Authentication = authentication;
        }
        #endregion

        #region Properties
        protected RemoteAuthentication _authentication;
        public RemoteAuthentication Authentication
        {
            get => this._authentication;
            set
            {
                bool edited = this._authentication != value;

                this._authentication = value;

                if (edited && this.Connected)
                {
                    this.Disconnect();
                    this.Connect();
                }
            }
        }

        public SftpClient _Client { get; private set; }
        #endregion

        #region IRemoteAccess
        public void Connect()
        {
            if (this.Connected)
                throw new AlreadyConnectedException();

            if (this._Client != null)
                this._Client.Dispose();

            this._Client = new SftpClient(this.Authentication.Address, this.Authentication.Username, this.Authentication.Password);

            try
            {
                this._Client.Connect();
            }
            catch (SshAuthenticationException ex)
            {
                throw new BadAuthenticationException("", ex.InnerException);
            }
        }

        public void Disconnect()
        {
            if (!this.Connected)
                throw new NotConnectedException();

            if (this._Client != null && this._Client.IsConnected)
                this._Client.Disconnect();
        }

        public bool Connected => this._Client != null && this._Client.IsConnected;

        #endregion

        #region IAccess
        public override void CreateDirectory(string path)
        {
            if (PathHelper.CorrectDirectorySeparators(path) == "/")
                return;

            if (!this.Connected)
                throw new NotConnectedException();

            if (this._Client.Exists(path))
                throw new DirectoryAlreadyExistsException();

            this._Client.CreateDirectory(path);
        }

        public override void DeleteDirectory(string path)
        {
            if (PathHelper.CorrectDirectorySeparators(path) == "/")
                return;

            if (!this.Connected)
                throw new NotConnectedException();

            if (!this.DirectoryExists(path))
                throw new DirectoryNotFoundException(path);

            this.EnsureDirectoryEmpty(path);
            this._Client.DeleteDirectory(path);
        }

        public override void CreateFile(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            this._Client.Create(path).Close();
        }

        public override void DeleteFile(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            this._Client.DeleteFile(path);
        }

        public override string ReadFile(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            long size = this._Client.ListDirectory(IO.Path.GetDirectoryName(path)).FirstOrDefault(x => IO.Path.GetFileName(path).ToLower() == x.Name.ToLower())?.Length ?? -1;

            //if (size == 0) - does not work
                //throw new NotFileException();
            if (size == -1)
                throw new FileNotFoundException();

            byte[] array = new byte[size];

            using (IO.MemoryStream stream = new IO.MemoryStream(array))
            {
                IO.StreamReader sr = new IO.StreamReader(stream);

                this._Client.DownloadFile(path, stream);
            }

            return string.Join("", Encoding.UTF8.GetString(array));
        }

        public override void WriteToFile(string path, string text)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            this._Client.WriteAllText(path, text, Encoding.UTF8);
        }

        public override void AppendToFile(string path, string text)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            this._Client.AppendAllText(path, text, Encoding.UTF8);
        }

        public override void CopyFile(string source, string destination, bool overwrite)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            this.EnsureDirectoryExists(IO.Path.GetDirectoryName(destination));

            using (IO.Stream file = IO.File.OpenRead(source))
            {
                this._Client.UploadFile(file, destination, overwrite);
            }
        }

        public override void RestoreFile(string source, string destination, bool overwrite = false)
        {
            if (!this.Connected)
                throw new NotConnectedException();
            IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(destination));

            if (!overwrite && IO.File.Exists(destination))
                throw new FileAlreadyExistsException(destination);

            using (IO.Stream stream = IO.File.OpenWrite(destination))
            {
                this._Client.DownloadFile(source, stream);
            }
        }

        public override Directory ListDirectory(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            List<string> directories = new List<string>();
            List<FileHistory> files = new List<FileHistory>();

            IEnumerable<SftpFile> stfpFiles = this._Client.ListDirectory(path);

            directories.AddRange(stfpFiles.Where(x => x.IsDirectory && x.Name != "." && x.Name != "..").Select(x => x.FullName));
            files.AddRange(stfpFiles.Where(x => !x.IsDirectory).Select(x =>
                new FileHistory()
                {
                    Path = x.FullName,
                    TimeStamp = x.LastWriteTimeUtc,
                }
            ));

            return new Directory(path, directories, files);
        }

        public override bool DirectoryExists(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            try
            {
                this._Client.ChangeDirectory(path);
                this._Client.ChangeDirectory("/");

                return true;
            }
            catch (SftpPathNotFoundException)
            {
                return false;
            }
        }

        public override bool FileExists(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            return this._Client.Exists(path);
        }
        #endregion
    }
}
