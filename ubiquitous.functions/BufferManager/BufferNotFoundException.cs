using System.Runtime.Serialization;

namespace ubiquitous.functions.BufferManager
{
    [Serializable]
    internal class BufferNotFoundException : Exception
    {
        public BufferNotFoundException()
        {
        }

        public BufferNotFoundException(string? message) : base(message)
        {
        }

        public BufferNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected BufferNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}