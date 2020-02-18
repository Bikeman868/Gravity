using Gravity.Server.Interfaces;
using OwinFramework.Utility.Containers;
using System;
using System.IO;

namespace Gravity.Server.Utility
{
    /// <summary>
    /// Implements a linked list of byte arrays where each byte array in
    /// the list contains a start and a length. This allows the byte array
    /// to have bytes inserted and deleted without having to copy all the
    /// bytes.
    /// </summary>
    internal class FlexibleByteArray
    {
    /*
        /// <summary>
        /// Used to obtain and recycle byte arrays
        /// </summary>
        private readonly IBufferPool _bufferPool;

        /// <summary>
        /// A linked list of the chunks of data in the array
        /// </summary>
        private readonly LinkedList<ByteBuffer> _buffers;

        /// <summary>
        /// The initial size of each new chunk of the array
        /// </summary>
        private readonly int _chunkSize;

        /// <summary>
        /// The total number of bytes in the array
        /// </summary>
        private int _count;

        public FlexibleByteArray(
            IBufferPool bufferPool, 
            int chunkSize)
        {
            _bufferPool = bufferPool;
            _chunkSize = chunkSize;
            _buffers = new LinkedList<ByteBuffer>();
        }

        public void Append(byte[] bytes, int offset, int count)
        {
            var lastBuffer = _buffers.LastOrDefault();

            if (lastBuffer != null && lastBuffer.CanAppend(count))
            {
                lastBuffer.Append(bytes, offset, count);
            }
            else
            {
                var buffer = new ByteBuffer(_bufferPool.GetAtLeast(count > _chunkSize ? count : _chunkSize), count);
                buffer.Append(bytes, offset, count);
                _buffers.Append(buffer);
            }

            _count += count;
        }

        public void Prepend(byte[] bytes, int offset, int count)
        {
            var firstBuffer = _buffers.FirstOrDefault();

            if (firstBuffer != null && firstBuffer.CanPrepend(count))
            {
                firstBuffer.Prepend(bytes, offset, count);
            }
            else
            {
                var buffer = new ByteBuffer(_bufferPool.GetAtLeast(count > _chunkSize ? count : _chunkSize), count);
                buffer.Prepend(bytes, offset, count);
                _buffers.Prepend(buffer);
            }

            _count += count;
        }

        public void GetBytes(
            int start, 
            int count, 
            byte[] destination, 
            int destinationOffset)
        {
            if (count < 1) return;

            var bufferListElement = _buffers.FirstElement();
            var byteBuffer = bufferListElement?.Data;

            while (byteBuffer != null && start >= byteBuffer.Length)
            {
                start -= byteBuffer.Length;
                bufferListElement = bufferListElement.Next;
                byteBuffer = bufferListElement?.Data;
            }

            do
            {
                if (byteBuffer == null)
                    throw new Exception("Attempt to read more data than there is in the flexible byte array");

                var bytesToCopy = count;

                if (start + bytesToCopy > byteBuffer.Length)
                    bytesToCopy = byteBuffer.Length - start;

                byteBuffer.Get(start, bytesToCopy, destination, destinationOffset);

                destinationOffset += bytesToCopy;
                count -= bytesToCopy;

                if (count == 0) return;

                bufferListElement = bufferListElement.Next;
                byteBuffer = bufferListElement?.Data;

                start = 0;
            } while (bufferListElement != null);
        }

        /// <summary>
        /// Replaces a section of buffered data with some new bytes
        /// </summary>
        /// <param name="bufferList">A list of data buffers</param>
        /// <param name="start">The byte offset to overwrite in the buffered data</param>
        /// <param name="length">How many bytes to delete from the buffers</param>
        /// <param name="replacementBytes">The new bytes to insert into the buffer</param>
        /// <param name="replacementOffset">An offset into replacementBytes to copy from</param>
        /// <param name="replacementLength">The number of bytes to copy from replacementBytes into the buffer</param>
        public void ReplaceBufferedBytes(
            int start, int length, 
            ref int unusedTailBytes,
            byte[] replacementBytes, int replacementOffset, int replacementLength)
        {
            if (length < 1) return;

            var bufferListElement = _buffers.FirstElement();
            var byteBuffer = bufferListElement?.Data;

            while (byteBuffer != null && start >= byteBuffer.Length)
            {
                start -= byteBuffer.Length;
                bufferListElement = bufferListElement.Next;
                byteBuffer = bufferListElement?.Data;
            }

            // Overwrite the overalpping bytes
            do
            {
                if (byteBuffer == null)
                    throw new Exception("Attempt to overwrite past the end of the buffer");

                int bytesToCopy;
                
                if (bufferListElement.Next == null && start + length > byteBuffer.Length - unusedTailBytes)
                {
                    bytesToCopy = replacementLength;

                    if (start + bytesToCopy > byteBuffer.Length)
                    {
                        bytesToCopy = byteBuffer.Length - start;
                        unusedTailBytes = 0;
                    }
                    else
                    {
                        unusedTailBytes = byteBuffer.Length - start - bytesToCopy;
                    }
                }
                else
                {
                    bytesToCopy = length;
                
                    if (bytesToCopy > replacementLength) 
                        bytesToCopy = replacementLength;

                    if (start + bytesToCopy > byteBuffer.Length) 
                        bytesToCopy = byteBuffer.Length - start;
                }

                Array.Copy(replacementBytes, replacementOffset, byteBuffer, start, bytesToCopy);

                length -= bytesToCopy;
                replacementOffset += bytesToCopy;
                replacementLength -= bytesToCopy;

                if (length > 0 && replacementLength > 0)
                {
                    bufferListElement = bufferListElement.Next;
                    byteBuffer = bufferListElement.Data;
                    start = 0;
                }
                else
                {
                    start += bytesToCopy;
                    break;
                }
            } while (true);

            // Delete extra bytes if the replacement is shorter
            while (length > 0)
            {
                var bytesToDelete = length;

                if (start + bytesToDelete > byteBuffer.Length)
                {
                    bytesToDelete = byteBuffer.Length - start;
                    if (start == 0)
                    {
                        var bufferListElementToDelete = bufferListElement;
                        bufferListElement = bufferListElement.Prior;
                        bufferList.Delete(bufferListElementToDelete);
                        _bufferPool.Reuse(byteBuffer);
                    }
                    else
                    {
                        var dataToKeep = _bufferPool.Get(byteBuffer.Length - bytesToDelete);
                        Array.Copy(byteBuffer, 0, dataToKeep, 0, dataToKeep.Length);
                        bufferListElement.Data = dataToKeep;
                        _bufferPool.Reuse(byteBuffer);
                    }
                }
                else
                {
                    if (start == 0)
                    {
                        var dataToKeep = _bufferPool.Get(byteBuffer.Length - bytesToDelete);
                        Array.Copy(byteBuffer, length, dataToKeep, 0, dataToKeep.Length);
                        bufferListElement.Data = dataToKeep;
                        _bufferPool.Reuse(byteBuffer);
                    }
                    else
                    {
                        var dataBefore = _bufferPool.Get(start);
                        var dataAfter = _bufferPool.Get(byteBuffer.Length - start - length);
                        Array.Copy(byteBuffer, 0, dataBefore, 0, dataBefore.Length);
                        Array.Copy(byteBuffer, start + length, dataAfter, 0, dataAfter.Length);
                        bufferListElement.Data = dataBefore;
                        bufferListElement = bufferList.InsertAfter(bufferListElement, dataAfter);
                        _bufferPool.Reuse(byteBuffer);
                    }
                }

                length -= bytesToDelete;
                bufferListElement = bufferListElement?.Next ?? bufferList.FirstElementOrDefault();
                byteBuffer = bufferListElement?.Data;
                start = 0;
            }

            // Append extra bytes if the replacement is longer
            if (replacementLength > 0)
            {
                var additionalData = _bufferPool.Get(replacementLength);
                Array.Copy(replacementBytes, replacementOffset, additionalData, 0, replacementLength);

                if (bufferListElement.Next == null)
                    unusedTailBytes = 0;

                bufferList.InsertAfter(bufferListElement, additionalData);
            }
        }
*/
    }
}