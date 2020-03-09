using Gravity.Server.Interfaces;
using OwinFramework.Utility.Containers;
using System;
using System.Linq;

namespace Gravity.Server.Utility
{
    /// <summary>
    /// This is a linked list of ByteBuffer. It is designed to allow you to maintain a large
    /// array of bytes in memory and efficiently add, delete or update bytes anywhere in the
    /// array.
    /// Note that this class is designed to be used by a single thread.
    /// </summary>
    internal class InfiniteByteArray: IDisposable
    {
        private readonly IBufferPool _bufferPool;
        private readonly LinkedList<ByteBuffer> _buffers;

        public long Length { get; private set; }

        public InfiniteByteArray(
            IBufferPool bufferPool)
        {
            _bufferPool = bufferPool;
            _buffers = new LinkedList<ByteBuffer>();
        }

        public void Dispose()
        {
            var buffer = _buffers.PopFirst();
            while (buffer != null)
            {
                _bufferPool.Reuse(buffer.Data);
                buffer = _buffers.PopFirst();
            }
        }

        /// <summary>
        /// Returns a buffer that can be used to append data to the byte array
        /// </summary>
        /// <param name="minLength">The minimum buffer length needed</param>
        /// <param name="buffer">Returns the buffer to write bytes to</param>
        /// <param name="offset">Returns the offset into buffer to write</param>
        /// <param name="count">Returns the maximum number of bytes that can be written</param>
        /// <returns>A lambda methodd to call to say how many bytes were actually written</returns>
        public Action<int> GetAppendBuffer(int? minLength, out byte[] buffer, out int offset, out int count)
        {
            var last = _buffers.LastOrDefault();

            if (last == null || minLength.HasValue && last.TailSize < minLength.Value)
            {
                if (minLength.HasValue)
                    last = new ByteBuffer(_bufferPool.GetAtLeast(minLength.Value));
                else
                    last = new ByteBuffer(_bufferPool.Get());

                _buffers.Append(last);
            }

            buffer = last.Data;
            offset = last.End;
            count = last.TailSize;

            return byteCount =>
            {
                last.End += byteCount;
                Length += byteCount;
            };
        }

        /// <summary>
        /// Adds data to the end of the array
        /// </summary>
        public void Append(byte[] buffer, int start, int count)
        {
            var last = _buffers.LastOrDefault();

            if (last == null || last.TailSize < count)
            {
                last = new ByteBuffer(_bufferPool.GetAtLeast(count));
                _buffers.Append(last);
            }

            if (last.CanAppend(count))
            {
                last.Append(buffer, start, count);
            }
            else
            {
                var newBuffer = last.Insert(last.End, 0, _bufferPool, buffer, start, count);

                if (newBuffer != null)
                    _buffers.Append(newBuffer);
            }

            Length += count;
        }

        /// <summary>
        /// Adds data to the end of the array
        /// </summary>
        public void Append(ByteBuffer buffer)
        {
            _buffers.Append(buffer);
            Length += buffer.Length;
        }

        private void RecalculateLength()
        {
            Length = _buffers.Aggregate(0L, (length, listElement) => length + listElement.Data.Length);
        }
    }
}