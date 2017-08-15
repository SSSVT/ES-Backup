using System;
using System.Runtime.Serialization;

namespace ESBackupAndReplication
{
    public class NotDirectoryException : Exception
    {
        public NotDirectoryException()
        {
        }

        public NotDirectoryException(string message) : base(message)
        {
        }

        public NotDirectoryException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NotDirectoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
