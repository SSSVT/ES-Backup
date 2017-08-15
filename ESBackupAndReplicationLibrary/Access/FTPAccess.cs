using ESBackupAndReplication.Access.Interfaces;
using ESBackupAndReplication.Authentication;
using ESBackupAndReplication.Helpers;
using ESBackupAndReplication.Objects;
using System;
using System.Collections.Generic;
using System.Net;
using FluentFTP;
using IO = System.IO;

namespace ESBackupAndReplication.Access
{
    public class FTPAccess : AbAccess, IRemoteAccess
    {
        #region Constructors
        public FTPAccess(RemoteAuthentication authentication)
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

        protected FtpClient _Client { get; set; }
        #endregion

        #region IRemoteAccess
        public void Connect()
        {
            if (this.Connected)
                throw new AlreadyConnectedException();

            this._Client = new FtpClient()
            {
                Credentials = new NetworkCredential(this.Authentication.Username, this.Authentication.Password),
            };


            try
            {
                this._Client.Connect();
            }
            catch (Exception)
            {
                this._Client = null;
                throw;
            }
        }

        public void Disconnect()
        {
            if (!this.Connected)
                throw new NotConnectedException();

            this._Client?.Disconnect();
            this._Client?.Dispose();
        }

        public bool Connected => this._Client != null;
        #endregion

        #region IAccess
        public override void CreateDirectory(string path)
        {
            if (PathHelper.CorrectDirectorySeparators(path) == "/")
                return;

            if (!this.Connected)
                throw new NotConnectedException();

            if(this.DirectoryExists(path))
                throw new DirectoryAlreadyExistsException();

            if(this.FileExists(path))
                throw new NotDirectoryException();

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

            if(this.FileExists(path))
                throw new FileAlreadyExistsException();

            //TODO: fix
        }

        public override void DeleteFile(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            if(!this.FileExists(path))
                throw new FileNotFoundException();

            this._Client.DeleteFile(path);
        }

        public override string ReadFile(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            if(!this.FileExists(path))
                throw new FileNotFoundException();

            using (var stream = this._Client.OpenRead(path))
            {
                using (var reader = new IO.StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public override void WriteToFile(string path, string text)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            if(!this.FileExists(path))
                throw new FileAlreadyExistsException();

            AppendToFile(path, text);
        }

        public override void AppendToFile(string path, string text)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            using (var stream = this._Client.OpenWrite(path))
            {
                using (var writer = new IO.StreamWriter(stream))
                {
                    writer.Write(text);
                }
            }
        }

        public override void CopyFile(string source, string destination, bool overwrite = false)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            this.EnsureDirectoryExists(IO.Path.GetDirectoryName(destination));

            if (!overwrite && this.FileExists(destination))
                throw new FileAlreadyExistsException(destination);

            //TODO: check errors
            var exists = overwrite ? FtpExists.Overwrite : FtpExists.Skip;

            this._Client.UploadFile(destination, source, exists);
        }

        public override void RestoreFile(string source, string destination, bool overwrite = false)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(destination));

            if (!overwrite && IO.File.Exists(destination))
                throw new FileAlreadyExistsException(destination);

            //TODO: check errors
            this._Client.DownloadFile(source, destination, overwrite);
        }

        public override Directory ListDirectory(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            path = PathHelper.CorrectDirectorySeparators(path);

            List<string> directories = new List<string>();
            List<FileHistory> files = new List<FileHistory>();

            foreach (var item in this._Client.GetListing(path))
            {
                string itemPath = PathHelper.CorrectDirectorySeparators(item.FullName);

                switch (item.Type)
                {
                    case FtpFileSystemObjectType.Directory:
                        directories.Add(itemPath);
                        break;
                    case FtpFileSystemObjectType.File:
                        files.Add(new FileHistory()
                        {
                            Path = itemPath,
                            Root = path,
                            TimeStamp = item.Modified
                        });
                        break;
                }
            }

            return new Directory(path, directories, files);
        }

        public override bool DirectoryExists(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            return this._Client.DirectoryExists(path);
        }

        public override bool FileExists(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            return this._Client.FileExists(path);
        }
        #endregion
    }
}
