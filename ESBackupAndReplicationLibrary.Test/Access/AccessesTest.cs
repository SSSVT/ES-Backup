using ESBackupAndReplication;
using ESBackupAndReplication.Access;
using ESBackupAndReplication.Access.Interfaces;
using ESBackupAndReplication.Authentication;
using ESBackupAndReplication.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ESBackupAndReplicationLibrary.Test.Access
{
    [TestFixture]
    public class AccessesTest
    {
        private RemoteAuthentication authentication = new RemoteAuthentication()
        {
            Address = "127.0.0.1",
            Username = "test_user",
            Password = "pwd",
        };

        private IAccess access;

        [SetUp]
        public void Prepare_Local()
        {
            AccessTestHelper.PrepareLocalFiles();
        }

        [TearDown]
        public void Remove_Local()
        {
            AccessTestHelper.RemoveLocalFiles();
            try
            {
                access.DeleteDirectory("Remote");
            }
            catch (DirectoryAlreadyExistsException)
            {

            }

            if (access is IRemoteAccess)
                ((IRemoteAccess)access).Disconnect();
        }

        [Test]
        public void Local_Access_Test()
        {
            access = new LocalAccess();
            var testHelper = new AccessTestHelper(access);

            testHelper.RunTests();
        }

        [Test]
        public void Ftp_Access_Test()
        {
            access = new FTPAccess(authentication);
            ((IRemoteAccess)access).Connect();

            var testHelper = new AccessTestHelper(access);

            testHelper.RunTests();
        }

        [Test]
        public void Sftp_Access_Test()
        {
            access = new SFTPAccess(authentication);
            ((IRemoteAccess)access).Connect();

            var testHelper = new AccessTestHelper(access);

            testHelper.RunTests();
        }

        [Test]
        public void Scp_Access_Test()
        {
            access = new SCPAccess(authentication);
            ((IRemoteAccess)access).Connect();

            string currentDirectory = PathHelper.CorrectDirectorySeparators(Environment.CurrentDirectory);

            var testHelper = new AccessTestHelper(access);

            testHelper.RunTests();
        }
    }
}
