using ESBackupAndReplication.Access.Interfaces;
using ESBackupAndReplication.Authentication;
using ESBackupAndReplication.Helpers;
using ESBackupAndReplication.Objects;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using IO = System.IO;

namespace ESBackupAndReplication.Access
{
    public class SCPAccess : AbAccess, IRemoteAccess
    {
        #region Constructors
        public SCPAccess(RemoteAuthentication authentication)
        {
            this.Authentication = authentication;
        }
        #endregion

        #region Properties
        private RemoteAuthentication _authentication;
        public RemoteAuthentication Authentication
        {
            get => this._authentication;
            set
            {
                bool edited = this._authentication != value;

                this._authentication = value;

                if (edited && this._SshClient != null && this._SshClient.IsConnected)
                {
                    this.Disconnect();
                    this.Connect();
                }
            }
        }

        protected ScpClient _ScpClient { get; private set; }
        protected SshClient _SshClient { get; private set; }
        #endregion

        #region IRemoteAccess
        public void Connect()
        {
            if ((this._SshClient != null && this._SshClient.IsConnected) || (this._ScpClient != null && this._ScpClient.IsConnected))
                throw new AlreadyConnectedException();

            this._ScpClient = new ScpClient(this.Authentication.Address, this.Authentication.Username, this.Authentication.Password);
            this._SshClient = new SshClient(this.Authentication.Address, this.Authentication.Username, this.Authentication.Password);
            try
            {
                this._SshClient.Connect();
                this._ScpClient.Connect();
            }
            catch (SshAuthenticationException ex)
            {
                throw new BadAuthenticationException("", ex.InnerException);
            }

            this._SshClient.RunCommand("export TZ=UTC"); //Using UTC
        }

        public void Disconnect()
        {
            if (this._SshClient != null && this._SshClient.IsConnected)
                this._SshClient.Disconnect();
            if (this._ScpClient != null && this._ScpClient.IsConnected)
                this._ScpClient.Disconnect();
        }

        public bool Connected => this._ScpClient != null && this._ScpClient.IsConnected && this._SshClient != null && this._SshClient.IsConnected;

        #endregion

        #region IAccess
        public override void CreateDirectory(string path)
        {
            if (PathHelper.CorrectDirectorySeparators(path) == "/")
                return;

            if (!this.Connected)
                throw new NotConnectedException();

            if (this.DirectoryExists(path))
                throw new DirectoryAlreadyExistsException();

            this.ExecuteCommand($"mkdir { this.CorrectPath(path, true) }");
        }

        public override void DeleteDirectory(string path)
        {
            if (PathHelper.CorrectDirectorySeparators(path) == "/")
                return;

            if (!this.Connected)
                throw new NotConnectedException();

            if (!this.DirectoryExists(path))
                throw new DirectoryNotFoundException(path);

            this.ExecuteCommand($"rm -rf { this.CorrectPath(path, true) }");
        }

        public override void CreateFile(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            if (!this.DirectoryExists(PathHelper.GetParent(path)))
                throw new DirectoryNotFoundException();

            if (this.FileExists(path))
                throw new FileAlreadyExistsException();

            if (this.DirectoryExists(path))
                throw new NotFileException();

            try
            {
                this.ExecuteCommand($"touch { this.CorrectPath(path) }");
            }
            catch(UnknownException)
            {
                this.ExecuteCommand($"printf \"\" > { this.CorrectPath(path) }");
            }
        }

        public override void DeleteFile(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            this.ExecuteCommand($"rm { this.CorrectPath(path) }");
        }

        public override string ReadFile(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            SshCommand cmd = this.ExecuteCommand($"cat { this.CorrectPath(path) }");
            return cmd.Result;
        }

        public override void WriteToFile(string path, string text)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            this.ExecuteCommand($"printf { text } > { this.CorrectPath(path) }");
        }

        public override void AppendToFile(string path, string text)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            this.ExecuteCommand($"printf { text } >> { this.CorrectPath(path) }");
        }

        public override void CopyFile(string source, string destination, bool overwrite)
        {
            if (!this.Connected)
                throw new NotConnectedException();
            this.EnsureDirectoryExists(IO.Path.GetDirectoryName(destination));

            if (this.FileExists(destination) && !overwrite)
                throw new FileAlreadyExistsException(destination);

            this._ScpClient.Upload(new IO.FileInfo(source), destination);
        }

        public override void RestoreFile(string source, string destination, bool overwrite = false)
        {
            if (!this.Connected)
                throw new NotConnectedException();
            IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(destination));

            if (!overwrite && IO.File.Exists(destination))
                throw new FileAlreadyExistsException(destination);

            this._ScpClient.Download(source, new IO.FileInfo(destination));
        }

        public override Directory ListDirectory(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            try
            {
                string output = this.ExecuteCommand($"ls -l { this.CorrectPath(path, true) }").Result;

                string[] lines = output.Split('\n');

                if (lines.Length == 0)
                    return null;

                bool removeFirstLine = Regex.IsMatch(lines[0], "[a-zA-Z]+ [\\d]+");

                if (removeFirstLine)
                    output = string.Join("\n", lines.Skip(1));

                List<string> directories;
                List<FileHistory> files;

                try
                {
                    FTPHelper.ParseDirectoryInfo(path, output, out directories, out files); //Parse using FTPHelper - FTP LIST usually outputs Unix/Windows format, which is same as ls -l
                }
                catch
                {
                    string lsShort = this.ExecuteCommand($"ls { this.CorrectPath(path, true) }").Result;
                    return this.ListDirectoryUniversal(path, output, lsShort);
                }

                return new Directory(path, directories, files);
            }
            catch (FileNotFoundException ex)
            {
                throw new DirectoryNotFoundException(ex.Message, ex.InnerException);
            }
        }

        public override bool DirectoryExists(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();
            try
            {
                SshCommand cmd = this.ExecuteCommand($"cd { this.CorrectPath(path, true) }");
                this._SshClient.RunCommand($"cd -");

                return cmd.ExitStatus != -1;
            }
            catch (Exception ex) 
                when (ex is NotDirectoryException ||
                      ex is DirectoryNotFoundException ||
                      ex is FileNotFoundException)
            {
                return false;
            }
        }

        public override bool FileExists(string path)
        {
            if (!this.Connected)
                throw new NotConnectedException();

            try
            {
                SshCommand cmd = this.ExecuteCommand($"cat { this.CorrectPath(path, false) }");
                return true;
            }
            catch (Exception ex)
                when(ex is FileNotFoundException ||
                     ex is NotFileException)
            {
                return false;
            }
        }
        #endregion

        #region Protected
        protected string CorrectPath(string path, bool appendSlash = false)
        {
            return PathHelper.CorrectDirectorySeparators(path, '/').TrimEnd('/') + (appendSlash ? "/" : "");
        }
        #endregion

        #region Helpers
        protected SshCommand ExecuteCommand(string command)
        {
            SshCommand cmd = this._SshClient.RunCommand(command);
            this.CheckExceptions(cmd);
            return cmd;
        }

        protected void CheckExceptions(SshCommand command)
        {
            bool isError = true;
            string error = command.Error?.ToLower();
            if (error == "" || error == null)
            {
                error = command.Result;
                isError = false;
            }
            
            if (Regex.IsMatch(error, "permission(s)? den(y|ied)?", RegexOptions.IgnoreCase))
                throw new PermissionsException(command.CommandText + ": " + command.Error);
            if (Regex.IsMatch(error, "(file (path )?does not exist|file not found|no such file)", RegexOptions.IgnoreCase))
                throw new FileNotFoundException(command.CommandText + ": " + command.Error);
            if (Regex.IsMatch(error, "((directory|folder) not found|can't cd to)", RegexOptions.IgnoreCase))
                throw new DirectoryNotFoundException(command.CommandText + ": " + command.Error);
            if (Regex.IsMatch(error, "not a directory", RegexOptions.IgnoreCase))
                throw new NotDirectoryException(command.CommandText + ": " + command.Error);
            if (Regex.IsMatch(error, "is a directory", RegexOptions.IgnoreCase))
                throw new NotFileException(command.CommandText + ": " + command.Error);

            if(isError)
                throw new UnknownException(command.CommandText + ": " + command.Error);
        }

        protected Directory ListDirectoryUniversal(string basePath, string lsLong, string lsShort)
        {
            string[] shortSplitted = lsShort.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x != "" ? x.TrimEnd('\r') : x).ToArray(),
                     longSplitted = lsLong.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select((x, i) => x != "" ? x.TrimEnd('\r').Replace(shortSplitted[i], "") : x).ToArray();

            List<string> directories = new List<string>();
            List<FileHistory> files = new List<FileHistory>();

            int ind = 0;
            foreach(string @long in longSplitted)
            {
                string permissions = @long.Substring(0, 10);
                bool dir = permissions[0] == 'd';

                string[] temp = @long.Substring(11).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                string dateTime = string.Join(" ", temp.Skip(4).Take(3));
                string path = IO.Path.Combine(basePath, shortSplitted[ind]);
                if (dir)
                {
                    directories.Add(path);
                    ind++;
                    continue;
                }

                DateTime date = this.ParseDateTime(dateTime);

                files.Add(new FileHistory()
                {
                    Path = path,
                    TimeStamp = date
                });
                ind++;
            }

            return new Directory(basePath, directories, files);
        }

        protected DateTime ParseDateTime(string dateTime)
        {
            string[] splitted = dateTime.Split(' ');
            List<string> formats = new List<string>();

            foreach(string entry in splitted)
                formats.Add(this.GetFormat(entry));

            
            return DateTime.ParseExact(dateTime, string.Join(" ", formats), CultureInfo.InvariantCulture);
        }

        protected string GetFormat(string str)
        {
            Match dayMonthYearRegex = Regex.Match(str, @"^(\d{1,4})(.+)(\d{1,2})\2(\d{1,4})$");

            if (dayMonthYearRegex.Success)
            {
                string first = dayMonthYearRegex.Groups[1].Value,
                       second = dayMonthYearRegex.Groups[3].Value,
                       third = dayMonthYearRegex.Groups[4].Value,
                       separator = dayMonthYearRegex.Groups[2].Value;
                bool reverse = first.Length == 4;

                if (reverse)
                {
                    string temp = first;
                    first = third;
                    third = temp;
                }

                string ret;
                
                if (int.Parse(second) > 12)
                    ret = string.Format("{1}{0}{2}{0}yyyy", separator, new string('M', first.Length), new string('d', second.Length));
                else
                    ret = string.Format("{1}{0}{2}{0}yyyy", separator, new string('d', first.Length), new string('M', second.Length));

                if (reverse)
                    ret = string.Join("", ret.Reverse());

                return ret;
            }

            Match timeRegex = Regex.Match(str, @"^(\d{1,2}):(\d{1,2})$");

            if (timeRegex.Success)
            {
                string hour = new string('H', timeRegex.Groups[1].Length);
                string minute = new string('m', timeRegex.Groups[2].Length);
                return hour + ":" + minute;
            }

            Match yearRegex = Regex.Match(str, @"^\d{4}$");

            if (yearRegex.Success)
                return "yyyy";

            Match dayRegex = Regex.Match(str, @"^\d{1,2}$");

            if (dayRegex.Success)
                return str.Length > 1 ? "dd" : "d";

            Match monthRegex = Regex.Match(str, @"^\w{3}$");

            if (monthRegex.Success)
                return "MMM";

            return "";
        }
        #endregion
    }
}
