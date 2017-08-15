using ESBackupAndReplication.Objects;
using System.Collections.Generic;

namespace ESBackupAndReplication.FileManagers.Interfaces
{
    internal interface IFileManager
    {
        List<FileHistory> SearchForBackupedFiles(string path, string searchFilePattern = ".*", string searchDirectoryPattern = ".*");
        List<FileHistory> SearchForFiles(string path, string searchFilePattern = ".*", string searchDirectoryPattern = ".*");

        ComparedHistory CompareVersions(List<FileHistory> source, List<FileHistory> history);
        List<FileHistory> GetFilesHistory(List<BackupHistory> previousBackups);
        List<FileHistory> FilterFilesHistory(List<FileHistory> toFilter, bool setCompressed = false);

        void CopyFiles(List<FileHistory> files, string destination);
        void RestoreFiles(List<FileHistory> files, string destination);

        void WriteInLog(string path, List<FileHistory> files);
        List<FileHistory> GetDeletedFilesFromLog(string path);
    }
}
