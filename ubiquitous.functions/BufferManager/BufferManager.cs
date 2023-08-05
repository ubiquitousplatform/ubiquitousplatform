using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ubiquitous.functions.BufferManager
{
    internal class BufferManager
    {
        // This class contains a threadsafe collection of buffers that can be used to pass data between the host and the wasm instance
        // The buffers are allocated in a pool and are reused as needed
        // There is a maximum number of buffers that can be allocated at any given time
        // There is a maximum size of each buffer that is configured during the initialization of the buffer manager
        // The buffer manager is responsible for allocating and deallocating buffers
        // The buffer manager is responsible for tracking the state of each buffer
        // TODO: PERF: Consider using readonly spans instead of buffers to prevent copying memory.
        // TODO: set a lifetime on buffer objects and evict them after a certain timeframe to ensure the buffer pool isn't exhausted by requests that are never completed

        // This is not using MemoryCache because evictions are on a 20 second timer and we want to evict immediately after the request is completed, and 
        // because MemoryCache doesn't enforce a maximum size limit on the cache, it only evicts when the cache is full
        // and it's in a background thread so it can exceed the maximum size limit for a period of time

        private int _maxBufferSize;
        private int _maxBufferCount;
        private int _currentBufferCount;
        private readonly ConcurrentDictionary<Guid, byte[]> _buffers = new();

        public BufferManager(int maxBufferSize, int maxBufferCount)
        {
            _maxBufferCount = maxBufferCount;
            _maxBufferSize = maxBufferSize;
        }
        public Guid StoreValue(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException($"{nameof(bytes)} cannot be null. Specify a valid byte array.");
            }

            // check if value is larger than max buffer size and throw exception if it is
            if (bytes.Length > _maxBufferSize)
            {
                throw new ValueTooLargeException($"Value size is {bytes.Length} which exceeds buffer maximum size limit of {_maxBufferSize}");
            }   

            // check if there are any buffers available in the pool and throw exception if there are not
            if (_currentBufferCount > _maxBufferCount)
            {
                throw new BufferPoolExhaustedException($"BufferManager pool size is currently at the maximum size limit of {_maxBufferCount}");
            }

            // generate a random guid to use as the buffer id
            var bufferId = Guid.NewGuid();

            // reference the value from the buffer (not a copy)
            _buffers[bufferId] = bytes;

            // return the buffer id
            return bufferId;
        }

        public byte[] RetrieveValue(Guid bufferId)
        {
            if (bufferId == Guid.Empty)
            {
                throw new ArgumentException($"{nameof(bufferId)} cannot be Guid.Empty. Specify a valid Guid.");
            }
            // check if the buffer id exists in the pool and throw exception if it does not
            if (!_buffers.ContainsKey(bufferId))
            {
                throw new BufferNotFoundException($"Buffer with id {bufferId} was not found in the BufferManager pool, or has already been retrieved.");
            }
            // remove the buffer from the pool
            var wasSuccessful = _buffers.TryRemove(bufferId, out var buffer);
            if (!wasSuccessful || buffer == null)
            {
                throw new BufferNotFoundException($"Buffer with id {bufferId} was unable to be retrieved from the BufferManager pool. Either it does not exist, it has already been retrieved, or some other unhandled exception occurred.");
            }
            return buffer;
        }
    }
}
