using ESBackupAndReplication.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ESBackupAndReplication.Helpers
{
    public static class FTPHelper
    {
        #region PublicHelpers
        public static bool ParseDirectoryInfo(string basePath, string records, out List<string> directories, out List<FileHistory> files)
        {
            files = new List<FileHistory>();
            directories = new List<string>();

            string[] recordLines = records.Split(new string[] { PathHelper.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            if (recordLines.Length == 0)
                return false;
            //Get format
            int format = GetFormat(recordLines[0]);

            if (format == -1)
                return false;

            foreach (string record in recordLines)
            {
                bool dir = false;
                string name = null;
                DateTime lastWriteTime = DateTime.MinValue;
                if (format == 0 && (name = ParseUnixInfo(record, out dir, out lastWriteTime)) == null)
                    continue; //TODO: throw exception?
                else if (format == 1 && (name = ParseWindowsInfo(record, out dir, out lastWriteTime)) == null)
                    continue; //TODO: throw exception?

                string path = PathHelper.CorrectDirectorySeparators(Path.Combine(basePath, name));
                if (dir)
                    directories.Add(path);
                else
                    files.Add(new FileHistory()
                    {
                        Path = path,
                        TimeStamp = lastWriteTime,
                    });
            }

            return true;
        }
        #endregion

        #region DirectoryInfoParserHelpers
        private static int GetFormat(string record)
        {
            if (record.Length > 10 && Regex.IsMatch(record.Substring(0, 10), "(-|d)(-|r)(-|w)(-|x)(-|r)(-|w)(-|x)(-|r)(-|w)(-|x)"))
                return 0; //Unix
            else if (record.Length > 8 && Regex.IsMatch(record.Substring(0, 8), "[0-9][0-9]-[0-9][0-9]-[0-9][0-9]"))
                return 1; //Windows

            return -1; //Unknown
        }

        private static string ParseUnixInfo(string record, out bool directory, out DateTime lastWriteTime)
        {
            record = record.Trim();
            string permissions = record.Substring(0, 10);
            directory = permissions[0] == 'd';

            string[] temp = record.Substring(11).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            //0 = number of links
            //1 = owner name
            //2 = owner group
            //3 = file size
            //should be same every time

            //this will vary depending on last modification date
            //4 - 6 = time of last modification
            //7 = name

            string dateTime = string.Join(" ", temp.Skip(4).Take(3));

            record = string.Join(" ", temp.Skip(4 + 3));

            try { lastWriteTime = DateTime.ParseExact(dateTime, "MMM dd HH:mm", CultureInfo.InvariantCulture); }
            catch { lastWriteTime = DateTime.ParseExact(dateTime, "MMM dd yyyy", CultureInfo.InvariantCulture); }

            string name = record;

            return name;
        }

        private static string ParseWindowsInfo(string record, out bool directory, out DateTime lastWriteTime)
        {
            string date = record.Substring(0, 8);
            record = record.Substring(8).Trim();

            string time = record.Substring(0, 7);
            record = record.Substring(7).Trim();

            lastWriteTime = DateTime.Parse(date + " " + time);

            directory = record.StartsWith("<DIR>");

            if (directory)
                record = record.Substring(5).Trim();
            else
                record = record.Split(new char[] { ' ' }, 2)[1];

            string name = record;

            return name;
        }
        #endregion
    }
}
