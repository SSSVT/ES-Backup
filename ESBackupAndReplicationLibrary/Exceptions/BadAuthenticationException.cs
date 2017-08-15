using System;
using System.Runtime.Serialization;

namespace ESBackupAndReplication
{
    public class BadAuthenticationException : Exception
    {
        public BadAuthenticationException()
        {
        }

        public BadAuthenticationException(string message) : base(message)
        {
        }

        public BadAuthenticationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BadAuthenticationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
