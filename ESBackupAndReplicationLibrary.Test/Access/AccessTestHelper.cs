using ESBackupAndReplication;
using ESBackupAndReplication.Access.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using IO = System.IO;
using System.Text;

namespace ESBackupAndReplicationLibrary.Test.Access
{
    public class AccessTestHelper
    {
        public IAccess Access { get; }

        public AccessTestHelper(IAccess access)
        {
            Access = access;
        }

        static Random rand = new Random();

        public static string GetRandomString(int length = 20)
        {
            string text = "";
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            for(int i = 0; i < length; i++)
            {
                int index = rand.Next(chars.Length);
                text += chars[index];
            }

            return text;
        }

        public static void PrepareLocalFiles()
        {
            IO.Directory.CreateDirectory("bin/Local1");
            IO.Directory.CreateDirectory("bin/Local1/Local1");
            IO.Directory.CreateDirectory("bin/Local1/FromRemote");

            IO.File.WriteAllText("bin/Local1/File1.txt", GetRandomString());
            IO.File.WriteAllText("bin/Local1/File2.txt", GetRandomString());
        }

        public static void RemoveLocalFiles()
        {
            IO.Directory.Delete("bin/Local1", true);
        }

        public void RunTests()
        {
            try
            {
                Access.EnsureDirectoryExists("Remote");
                Access.EnsureDirectoryEmpty("Remote");

                Create_Directory_Check("Remote/Remote1");
                Create_Directory_Not_Parent("Remote/Remote2/Remote1");

                Create_Directory_Check("Remote/Remote3");

                Create_File_Check("Remote/Remote1/createFile.txt");
                Create_File_Not_Parent("Remote/Remote2/createFile.txt");

                Delete_File_Check("Remote/Remote1/createFile.txt");
                Delete_Directory_Check("Remote/Remote3");

                string temp = GetRandomString();
                Write_Read("Remote/Remote1/file.txt", temp);
                Append_Read("Remote/Remote1/file.txt", temp, GetRandomString());

                Write_Append_Directory_Not_Exists("Remote/Remote2/file.txt", GetRandomString());

                Copy_File("bin/Local1/File1.txt", "Remote/File1.txt");
                Copy_File("bin/Local1/File2.txt", "Remote/File2.txt");

                Copy_File_Dst_Exists("bin/Local1/File1.txt", "Remote/File1.txt");
                Copy_File_Src_Not_Exists("bin/Local1/FileNotExists.txt", "Remote/Remote1/FileNotExists.txt");

                Restore_File("Remote/File1.txt", "bin/Local1/FromRemote/File1.txt");
                Restore_File("Remote/File2.txt", "bin/Local1/FromRemote/File2.txt");

                Restore_File_Dst_Exists("Remote/File2.txt", "bin/Local1/FromRemote/File2.txt");
                Restore_File_Src_Not_Exists("Remote/FileNotExists.txt", "bin/Local1/FromRemote/File3.txt");
            }
            finally
            {
                Access.EnsureDirectoryEmpty("Remote");
            }
        }

        public void Write_Read(string path, string text)
        {
            Access.WriteToFile(path, text);
            Assert.AreEqual(text, Access.ReadFile(path));
        }

        public void Append_Read(string path, string old, string append)
        {
            Access.AppendToFile(path, append);
            Assert.AreEqual(old + append, Access.ReadFile(path));
        }

        public void Write_Append_Directory_Not_Exists(string path, string text)
        {
            Assert.Throws<DirectoryNotFoundException>(() => Access.WriteToFile(path, text));
            Assert.Throws<DirectoryNotFoundException>(() => Access.AppendToFile(path, text));
        }

        public void Create_Directory_Check(string path)
        {
            Access.CreateDirectory(path);
            Assert.That(Access.DirectoryExists(path));

            Assert.Throws<DirectoryAlreadyExistsException>(() => Access.CreateDirectory(path));
        }

        public void Create_Directory_Not_Parent(string path)
        {
            //Assert.Throws<DirectoryNotFoundException>(() => Access.CreateDirectory(path));
        }

        public void Delete_Directory_Check(string path)
        {
            Access.DeleteDirectory(path);
            Assert.False(Access.DirectoryExists(path));
            Assert.Throws<DirectoryNotFoundException>(() => Access.DeleteDirectory(path));
        }

        public void Create_File_Check(string path)
        {
            Access.CreateFile(path);
            Assert.That(Access.FileExists(path));

            Assert.Throws<FileAlreadyExistsException>(() => Access.CreateFile(path));
        }

        public void Create_File_Not_Parent(string path)
        {
            Assert.Throws<DirectoryNotFoundException>(() => Access.CreateFile(path));
        }

        public void Delete_File_Check(string path)
        {
            Access.DeleteFile(path);
            Assert.False(Access.FileExists(path));
            Assert.Throws<FileNotFoundException>(() => Access.DeleteFile(path));
        }

        public void Copy_File(string source, string destination)
        {
            Access.CopyFile(source, destination);
            Assert.AreEqual(IO.File.ReadAllText(source), Access.ReadFile(destination));
        }

        public void Copy_File_Src_Not_Exists(string source, string destination)
        {
            Assert.Throws<FileNotFoundException>(() => Access.CopyFile(source, destination));
        }

        public void Copy_File_Dst_Exists(string source, string destination)
        {
            Assert.Throws<FileAlreadyExistsException>(() => Access.CopyFile(source, destination));

            Assert.DoesNotThrow(() => Access.CopyFile(source, destination, true));
        }

        public void Restore_File(string source, string destination)
        {
            Access.RestoreFile(source, destination);
            Assert.AreEqual(IO.File.ReadAllText(destination), Access.ReadFile(source));
        }

        public void Restore_File_Src_Not_Exists(string source, string destination)
        {
            Assert.Throws<FileNotFoundException>(() => Access.RestoreFile(source, destination));
        }

        public void Restore_File_Dst_Exists(string source, string destination)
        {
            Assert.Throws<FileAlreadyExistsException>(() => Access.RestoreFile(source, destination));

            Assert.DoesNotThrow(() => Access.RestoreFile(source, destination, true));
        }

        public void List_Check()
        {
            //TODO: implement this
        }
    }
}
