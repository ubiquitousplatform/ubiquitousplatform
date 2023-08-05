using System.Runtime.Serialization;

namespace ubiquitous.functions.BufferManager
{
    [Serializable]
    internal class BufferPoolExhaustedException : Exception
    {
        public BufferPoolExhaustedException()
        {
        }

        public BufferPoolExhaustedException(string? message) : base(message)
        {
        }

        public BufferPoolExhaustedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected BufferPoolExhaustedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}