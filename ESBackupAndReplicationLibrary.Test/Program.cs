using System;
using System.Reflection;
using NUnitLite;

namespace ESBackupAndReplicationLibrary.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            new AutoRun(Assembly.GetEntryAssembly()).Execute(args);
        }
    }
}
