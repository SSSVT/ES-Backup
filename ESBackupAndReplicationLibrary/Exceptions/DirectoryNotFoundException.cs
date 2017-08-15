using System;
using System.Runtime.Serialization;

namespace ESBackupAndReplication
{
    public class DirectoryNotFoundException : Exception
    {
        public DirectoryNotFoundException()
        {
        }

        public DirectoryNotFoundException(string message) : base(message)
        {
        }

        public DirectoryNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DirectoryNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
