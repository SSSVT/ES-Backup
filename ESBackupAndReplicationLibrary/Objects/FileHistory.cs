using ESBackupAndReplication.Helpers;
using System;
using System.Text.RegularExpressions;

namespace ESBackupAndReplication.Objects
{
    public class FileHistory
    {
        #region Static
        public static FileHistory Deserialize(string value, string root, bool relative = true)
        {
            string prefix = "";
            if (Regex.IsMatch(value, "^\\\"[a-zA-Z]:"))
            {
                prefix = value.Substring(1, 2);
                value = value.Substring(0, 1) + value.Substring(4);
            }

            string[] arr = value.Split(new char[] { ':' }, 2);
            string path = prefix + arr[0].Replace("\"", "");
            FileHistory file = new FileHistory()
            {
                Root = root,
                TimeStamp = DateTime.Parse(arr[1].Replace("\"", "")),
            };

            if (relative)
                file.RelativePath = path;
            else
                file.Path = path;

            return file;
        }
        #endregion

        #region Files
        protected string _path, _root;
        #endregion

        #region Properties
        public DateTime TimeStamp { get; set; }
        public bool Compressed { get; set; }

        public string Path
        {
            get => this._path;
            set => this._path = PathHelper.CorrectDirectorySeparators(value);
        }

        public string RelativePath
        {
            get
            {
                if (this.Root == null)
                    return null;

                return this.GetRelativePath(this.Root);
            }
            set => this._path = PathHelper.CorrectDirectorySeparators(System.IO.Path.Combine(this.Root, value));
        }
        
        public string Root
        {
            get => this._root;
            set => this._root = PathHelper.CorrectDirectorySeparators(value);
        }
        #endregion

        #region Methods
        public string Serialize()
        {
            return $"\"{ this.RelativePath }\":\"{ this.TimeStamp }\"";
        }

        public string GetRelativePath(string root = null)
        {
            if (root == null)
                return this.RelativePath;

            return PathHelper.GetRelativePath(root, this.Path);
        }

        public void UpdateRoot(string root, bool updatePath = true)
        {
            if (updatePath)
                this.Path = System.IO.Path.Combine(root, this.RelativePath);
            this.Root = root;
        }
        #endregion
    }
}
