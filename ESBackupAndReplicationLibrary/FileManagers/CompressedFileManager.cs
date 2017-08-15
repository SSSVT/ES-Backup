using System;
using System.Collections.Generic;
using System.Linq;
using ESBackupAndReplication.Objects;
using System.IO.Compression;
using System.IO;
using ESBackupAndReplication.Helpers;
using ESBackupAndReplication.Access.Interfaces;
using System.Text.RegularExpressions;

namespace ESBackupAndReplication.FileManagers
{
    internal class CompressedFileManager : AbFileManager
    {
        #region Fields
        protected string Extension = ".zip";
        #endregion

        #region Constructors
        public CompressedFileManager(IAccess access, string logFile, string strucureFile) : base(access, logFile)
        {
            this.StructureFile = strucureFile;
        }
        #endregion

        #region Properties
        public string StructureFile { get; set; }
        #endregion

        #region IFileManager
        public override void CopyFiles(List<FileHistory> files, string destination)
        {
            destination = this.AppendExtension(destination);

            string temp = Path.GetTempFileName();
            string toLog = "";
            using (Stream stream = File.OpenWrite(temp))
            {
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    foreach (FileHistory item in files)
                    {
                        archive.CreateEntryFromFile(item.Path, item.RelativePath);
                        toLog += item.Serialize() + PathHelper.NewLine;
                    }
                }
            }
            this.Access.CopyFile(temp, destination);
            File.Delete(temp);

            string writeLogPath = destination + "_" + this.StructureFile;
            this.Access.WriteToFile(writeLogPath, toLog);
        }

        public override void RestoreFiles(List<FileHistory> files, string destination)
        {
            files.OrderBy(x => x.Root);

            foreach (var groupedFiles in files.GroupBy(x => x.Root))
            {
                string root = groupedFiles.First().Root;
                string temp = Path.GetTempFileName();

                this.Access.RestoreFile(root, temp, true);

                using(ZipArchive archive = ZipFile.OpenRead(temp))
                {
                    foreach (FileHistory file in groupedFiles)
                    {
                        ZipArchiveEntry entry = archive.Entries.FirstOrDefault(x => String.Equals(x.FullName, file.RelativePath, StringComparison.InvariantCulture));

                        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(destination, file.RelativePath)));
                        string path = Path.Combine(destination, file.RelativePath);
                        entry.ExtractToFile(path, true);
                    }
                }

                File.Delete(temp);
            }
        }

        public override List<FileHistory> SearchForBackupedFiles(string path, string searchFilePattern = ".*", string searchDirectoryPattern = ".*")
        {
            path = this.AppendExtension(path);
            List<FileHistory> list = new List<FileHistory>();

            string writeLogPath = path + "_" + this.StructureFile;
            string[] read = this.Access.ReadFile(writeLogPath).Split(new string[] { PathHelper.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in read)
            {
                FileHistory deserialized = FileHistory.Deserialize(line, path);
                string[] directories = PathHelper.CorrectDirectorySeparators(Path.GetDirectoryName(deserialized.RelativePath), '/').Split('/');
                string fileName = Path.GetFileName(deserialized.RelativePath);

                if (!Regex.IsMatch(fileName, searchFilePattern))
                    continue;

                bool failed = false;
                foreach(string directoryName in directories)
                {
                    if (!Regex.IsMatch(directoryName, searchDirectoryPattern))
                    {
                        failed = true;
                        break;
                    }
                }
                if (failed)
                    continue;

                list.Add(deserialized);
            }

            return list;
        }
        #endregion

        #region AbFileManager
        protected override string GetLogPath(string destination)
        {
            return destination + "_" + this.LogFile;
        }

        protected override bool IsCompressed()
        {
            return true;
        }
        #endregion

        #region Protected
        protected string AppendExtension(string path)
        {
            if (!path.ToLower().EndsWith(this.Extension.ToLower()))
                path += this.Extension;

            return path;
        }
        #endregion
    }
}
