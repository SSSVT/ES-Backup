using System;
using System.Runtime.Serialization;

namespace ESBackupAndReplication
{
    public class NotConnectedException : Exception
    {
        public NotConnectedException()
        {
        }

        public NotConnectedException(string message) : base(message)
        {
        }

        public NotConnectedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NotConnectedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
