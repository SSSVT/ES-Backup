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
                Host = this.Authentication.Address,
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

            try
            {
                this._Client.CreateDirectory(path);
            }
            catch(FtpCommandException e)
            {
                if (e.CompletionCode == "550")
                    throw new PermissionsException();
            }
        }

        public override void DeleteDirectory(string path)
        {
            if (PathHelper.CorrectDirectorySeparators(path) == "/")
                return;

            if (!this.Connected)
                throw new NotConnectedException();

            if (!this.DirectoryExists(path))
                throw new DirectoryNotFoundException(path);

            try
            { 
                this.EnsureDirectoryEmpty(path);
                this._Client.DeleteDirectory(path);
            }
            catch (FtpCommandException e)
            {
                if (e.CompletionCode == "550")
                    throw new PermissionsException();
            }
        }

        public override void CreateFile(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            if(this.FileExists(path))
                throw new FileAlreadyExistsException();

            WriteToFile(path, "");
        }

        public override void DeleteFile(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            if(!this.FileExists(path))
                throw new FileNotFoundException();

            try
            { 
                this._Client.DeleteFile(path);
            }
            catch (FtpCommandException e)
            {
                if (e.CompletionCode == "550")
                    throw new PermissionsException();
            }
        }

        public override string ReadFile(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            if(!this.FileExists(path))
                throw new FileNotFoundException();

            try
            {
                using (var stream = this._Client.OpenRead(path))
                {
                    using (var reader = new IO.StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (FtpCommandException e)
            {
                if (e.CompletionCode == "550")
                    throw new PermissionsException();
            }

            return null;
        }

        public override void WriteToFile(string path, string text)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            AppendToFile(path, text);
        }

        public override void AppendToFile(string path, string text)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            if (!DirectoryExists(PathHelper.GetParent(path)))
                throw new DirectoryNotFoundException();

            try
            {
                using (var stream = this._Client.OpenAppend(path, FtpDataType.ASCII, false))
                {
                    using (var writer = new IO.StreamWriter(stream))
                    {
                        writer.Write(text);
                    }
                }
            }
            catch (FtpCommandException e)
            {
                if (e.CompletionCode == "550")
                    throw new PermissionsException();
            }
        }

        public override void CopyFile(string source, string destination, bool overwrite = false)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            this.EnsureDirectoryExists(IO.Path.GetDirectoryName(destination));

            if (!IO.File.Exists(source))
                throw new FileNotFoundException();

            if (!overwrite && this.FileExists(destination))
                throw new FileAlreadyExistsException(destination);

            //TODO: check errors
            try
            { 
                var exists = overwrite ? FtpExists.Overwrite : FtpExists.Skip;

                this._Client.UploadFile(source, destination, exists);
            }
            catch (FtpCommandException e)
            {
                if (e.CompletionCode == "550")
                    throw new PermissionsException();
            }
        }

        public override void RestoreFile(string source, string destination, bool overwrite = false)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(destination));

            if (!FileExists(source))
                throw new FileNotFoundException();

            if (!overwrite && IO.File.Exists(destination))
                throw new FileAlreadyExistsException(destination);

            //TODO: check errors
            try
            {
                this._Client.DownloadFile(destination, source, overwrite);
            }
            catch (FtpCommandException e)
            {
                if (e.CompletionCode == "550")
                    throw new PermissionsException();
            }
        }

        public override Directory ListDirectory(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            path = PathHelper.CorrectDirectorySeparators(path);

            List<string> directories = new List<string>();
            List<FileHistory> files = new List<FileHistory>();

            try
            {
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
            catch (FtpCommandException e)
            {
                if (e.CompletionCode == "550")
                    throw new PermissionsException();
            }

            return null;
        }

        public override bool DirectoryExists(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            try
            { 
                return this._Client.DirectoryExists(path);
            }
            catch (FtpCommandException e)
            {
                if (e.CompletionCode == "550")
                    throw new PermissionsException();
            }

            return false;
        }

        public override bool FileExists(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            try
            {
                return this._Client.FileExists(path);
            }
            catch (FtpCommandException e)
            {
                if (e.CompletionCode == "550")
                    throw new PermissionsException();
            }

            return false;
        }
        #endregion
    }
}
