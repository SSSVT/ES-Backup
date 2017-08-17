using ESBackupAndReplication.Objects;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ESBackupAndReplicationLibrary.Test.Objects
{
    [TestFixture]
    public class FileHistoryTest
    {
        private FileHistory fileHistory;
        private string relativePath = "file.txt", root = "/home/user/a/b/c/d/e";

        [SetUp]
        public void CreateFileHistory()
        {
            fileHistory = new FileHistory()
            {
                Root = root,
                RelativePath = relativePath
            };
        }

        [Test]
        [TestCase("/home", "user/a/b/c/d/e/file.txt")]
        [TestCase("/home/user", "a/b/c/d/e/file.txt")]
        [TestCase("/home/user/a", "b/c/d/e/file.txt")]
        public void RelativePath_WhenRootChanged_ShouldReturnCorrectRelativePath(string root, string relativePath)
        {
            fileHistory.Root = root;
            Assert.AreEqual(relativePath, fileHistory.RelativePath);
        }

        [Test]
        [TestCase("/home", "/home/file.txt")]
        [TestCase("/usr", "/usr/file.txt")]
        public void UpdateRoot_WhenCalledUpdatePath_ShouldChangePath(string root, string path)
        {
            fileHistory.UpdateRoot(root, true);
            Assert.AreEqual(path, fileHistory.Path);
        }

        [Test]
        [TestCase("/home")]
        [TestCase("/home/user")]
        [TestCase("/home/user/a")]
        public void UpdateRoot_WhenCalledNotUpdatePath_ShouldNotChangePath(string root)
        {
            string oldPath = fileHistory.Path;
            fileHistory.UpdateRoot(root, false);
            Assert.AreEqual(oldPath, fileHistory.Path);
        }
    }
}
