using System.Runtime.Serialization;

namespace ubiquitous.functions.BufferManager
{
    [Serializable]
    internal class ValueTooLargeException : Exception
    {
        public ValueTooLargeException()
        {
        }

        public ValueTooLargeException(string? message) : base(message)
        {
        }

        public ValueTooLargeException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ValueTooLargeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}