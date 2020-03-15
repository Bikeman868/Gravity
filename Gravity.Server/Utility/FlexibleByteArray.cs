using Gravity.Server.Interfaces;
using OwinFramework.Utility.Containers;
using System;
using System.Linq;

namespace Gravity.Server.Utility
{
    /// <summary>
    /// This is a linked list of ByteBuffer. It is designed to allow you to maintain a large
    /// array of bytes in memory and efficiently insert, delete or update bytes anywhere in the
    /// array.
    /// Note that this class is designed to be used by a single thread.
    /// </summary>
    internal class FlexibleByteArray: IDisposable
    {
        private readonly IBufferPool _bufferPool;
        private readonly LinkedList<ByteBuffer> _buffers;

        public long Length { get; private set; }

        public FlexibleByteArray(
            IBufferPool bufferPool)
        {
            _bufferPool = bufferPool;
            _buffers = new LinkedList<ByteBuffer>();
        }

        public void Dispose()
        {
            Clear();
        }

        /// <summary>
        /// Deletes all of the data in the array
        /// </summary>
        public void Clear()
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

        /// <summary>
        /// Provides random access to individual bytes
        /// </summary>
        public byte this[long index]
        {
            get 
            {
                var bufferElement = FindBufferAt(index, out var start);
                return bufferElement.Data.Data[index - start];
            }
            set
            {
                var bufferElement = FindBufferAt(index, out var start);
                bufferElement.Data.Data[index - start] = value;
            }
        }

        /// <summary>
        /// Returns a buffer that can be used to read data out of the byte array
        /// </summary>
        /// <param name="index">The offset into the array to start reading data</param>
        /// <param name="buffer">The buffer containing bytes to read</param>
        /// <param name="bufferOffset">The offset into the buffer to start reading</param>
        /// <param name="bufferCount">The number of bytes available in this buffer</param>
        public void GetReadBuffer(long index, out byte[] buffer, out int bufferOffset, out int bufferCount)
        {
            var bufferElement = FindBufferAt(index, out var startIndex);
            var byteBuffer = bufferElement.Data;
            buffer = byteBuffer.Data;

            bufferOffset = (int)(index - startIndex);
            bufferCount = byteBuffer.End - bufferOffset;
        }

        /// <summary>
        /// Removes a range of bytes from the array
        /// </summary>
        public void Delete(long index, int count)
        {
            if (count <= 0) return;
            if (index < 0) throw new IndexOutOfRangeException($"Array index {index} is cannot be negative");

            var startIndex = 0L;
            var bufferElement = _buffers.FirstElementOrDefault();
            do
            {
                if (bufferElement == null) return;

                var buffer = bufferElement.Data;

                if (startIndex + buffer.Length > index)
                {
                    var offset = (int)(index - startIndex);
                    var remaining = count;
                    while (remaining > 0)
                    {
                        if (bufferElement == null) break;
                        buffer = bufferElement.Data;

                        if (offset == 0 && remaining >= buffer.Length)
                        {
                            remaining -= buffer.Length;
                            var thisElement = bufferElement;
                            bufferElement = thisElement.Next;
                            _buffers.Delete(thisElement);
                        }
                        else
                        {
                            buffer.Delete(ref offset, ref remaining);
                            bufferElement = bufferElement.Next;
                        }
                    }
                    Length -= count - remaining;
                    return;
                }

                startIndex += buffer.Length;
                bufferElement = bufferElement.Next;
            } while (true);
        }

        /// <summary>
        /// Inserts bytes into the array
        /// </summary>
        /// <param name="index">The index position to insert data at</param>
        /// <param name="data">The data to insert</param>
        /// <param name="offset">The offset within data to srart copying bytes</param>
        /// <param name="count">The number of bytes to insert</param>
        public void Insert(long index, byte[] data, int offset, int count)
        {
            if (count <= 0) return;

            if (index >= Length)
            {
                Append(data, offset, count);
                return;
            }
         
            var bufferElement = FindBufferAt(index, out var bufferStartIndex);
            bufferElement.Data.Insert((int)(index - bufferStartIndex), 0, _bufferPool, data, offset, count);
            Length += count;
        }

        /// <summary>
        /// Replaces bytes in the array with another set of bytes (can be shorter or longer)
        /// </summary>
        /// <param name="index">The index position to insert data at</param>
        /// <param name="bytesToOverwrite">The number of existing bytes in the array to overwrite/delete</param>
        /// <param name="data">The data to write into the array</param>
        /// <param name="offset">The offset within data to srart copying bytes</param>
        /// <param name="count">The number of bytes to copy into the array from data</param>
        public void Replace(long index, int bytesToOverwrite, byte[] data, int offset, int count)
        {
            if (count <= 0) return;

            if (index >= Length)
            {
                Append(data, offset, count);
                return;
            }

            Length += count - bytesToOverwrite;

            var bufferElement = FindBufferAt(index, out var bufferStartIndex);
            var bufferIndex = (int)(index - bufferStartIndex);
            while (bufferElement != null && count > 0)
            {
                bufferElement.Data.Replace(data, ref bufferIndex, ref bytesToOverwrite, ref offset, ref count);
                bufferElement = bufferElement.Next;
            }
        }

        private void RecalculateLength()
        {
            Length = _buffers.Aggregate(0L, (length, listElement) => length + listElement.Data.Length);
        }

        private LinkedList<ByteBuffer>.ListElement FindBufferAt(long index, out long startIndex)
        {
            if (index < 0)
                throw new IndexOutOfRangeException($"Array index {index} is cannot be negative");

            startIndex = 0L;
            var bufferElement = _buffers.FirstElementOrDefault();
            do
            {
                if (bufferElement == null)
                    throw new IndexOutOfRangeException($"Array index {index} is beyond the end of the array");

                var buffer = bufferElement.Data;

                if (startIndex + buffer.Length > index)
                    return bufferElement;

                startIndex += buffer.Length;
                bufferElement = bufferElement.Next;
            } while (true);
        }
    }
}