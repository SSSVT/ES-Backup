using ESBackupAndReplication.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ESBackupAndReplicationLibrary.Test.Helpers
{
    [TestFixture]
    public class PathHelperTest
    {
        [Test]
        [TestCase("/home", "/home/user/documents/files", "user/documents/files")]
        [TestCase("/home", "/home/user/documents/files/", "user/documents/files/")]
        [TestCase("/usr/lib", "/usr/lib/apache2/modules/mod_cgi.so", "apache2/modules/mod_cgi.so")]
        [TestCase("/", "/usr/lib", "usr/lib")]
        public void GetRelativePath_WhenCalled_ShouldReturnRelativePath(string root, string absolute, string relative)
        {
            Assert.AreEqual(relative, PathHelper.GetRelativePath(root, absolute));
        }

        [Test]
        [TestCase("/home/user/documents/files/movies.txt", "/home/user/documents/files")]
        [TestCase("/home/user/documents/files/", "/home/user/documents")]
        [TestCase("/home/user/documents/files", "/home/user/documents")]
        public void GetParent_WhenCalled_ShouldReturnParentDirectory(string path, string parent)
        {
            Assert.AreEqual(parent, PathHelper.GetParent(path));
        }

        [Test]
        [TestCase("a\\b\\c\\d\\e\\f", '/', "a/b/c/d/e/f")]
        [TestCase("a\\b/c\\d\\e/f", '\'', "a'b'c'd'e'f")]
        public void CorrectDirectorySeparators_WhenCalled_ShouldReplaceAllSeparatorsWithX(string path, char x, string result)
        {
            Assert.AreEqual(result, PathHelper.CorrectDirectorySeparators(path, x));
        }
    }
}
