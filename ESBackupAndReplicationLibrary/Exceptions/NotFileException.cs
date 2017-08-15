using System;
using System.Runtime.Serialization;

namespace ESBackupAndReplication
{
    public class NotFileException : Exception
    {
        public NotFileException()
        {
        }

        public NotFileException(string message) : base(message)
        {
        }

        public NotFileException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NotFileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
