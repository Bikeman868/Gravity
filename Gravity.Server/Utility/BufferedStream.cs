﻿using Gravity.Server.Interfaces;
using OwinFramework.Utility.Containers;
using System;
using System.IO;

namespace Gravity.Server.Utility
{
    /// <summary>
    /// Objects of this type can wrap a stream to provide random 
    /// access to some small part of the stream so that the stream content
    /// can be transformed in some way as the data flows through the
    /// stream without keeping the whole stream in memory
    /// </summary>
    internal class BufferedStream : Stream
    {
        private readonly Stream _stream;
        private readonly IBufferPool _bufferPool;
        private readonly int _readBufferLength;
        private readonly int _writeBufferLength;
        private readonly LinkedList<byte[]> _readBuffers;
        private readonly LinkedList<byte[]> _writeBuffers;
        private readonly object _readLock = new object();
        private readonly object _writeLock = new object();

        /// <summary>
        /// True after we read 0 bytes from the stream
        /// </summary>
        private bool _endOfReadStream;

        /// <summary>
        /// The number of bytes currently in _readBuffers
        /// </summary>
        private int _readCount;

        /// <summary>
        /// The number of bytes currently in _writeBuffers
        /// </summary>
        private int _writeCount;

        /// <summary>
        /// The number of bytes at the front of the _readBuffers that have already been read
        /// </summary>
        private int _readHeadPosition;

        /// <summary>
        /// The number of bytes at the back of the _readBuffers that have nt yet been filled with data
        /// </summary>
        private int _readTailPosition;

        /// <summary>
        /// Constructs a wrapper around a source stream
        /// </summary>
        /// <param name="stream">The underlying stream to wrap</param>
        /// <param name="bufferPool">Pools and reused byte arrays</param>
        /// <param name="readBufferLength">The number of bytes read from the stream that must be kept in memory for incoming stream processing</param>
        /// <param name="writeBufferLength">The number of bytes to hold back from being written to the stream until the outgoing stream has been processed</param>
        public BufferedStream(
            Stream stream, 
            IBufferPool bufferPool, 
            int readBufferLength, 
            int writeBufferLength)
        {
            _stream = stream;
            _bufferPool = bufferPool;
            _readBufferLength = readBufferLength;
            _writeBufferLength = writeBufferLength;

            if (readBufferLength > 0)
                _readBuffers = new LinkedList<byte[]>();

            if (writeBufferLength > 0)
                _writeBuffers = new LinkedList<byte[]>();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _stream.Dispose();
            base.Dispose(disposing);
        }

        public override void Close()
        {
            if (_writeBuffers != null)
            {
                var writeBuffer = _writeBuffers.PopFirst();

                while (writeBuffer != null)
                {
                    _stream.Write(writeBuffer, 0, writeBuffer.Length);
                    _bufferPool.Reuse(writeBuffer);
                    writeBuffer = _writeBuffers.PopFirst();
                }

                _stream.Flush();
            }

            _stream.Close();
            base.Close();
        }

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => _stream.Length;

        /// <summary>
        /// The absolute byte offset into the read stream where the first byte of buffered data is available
        /// </summary>
        public long BufferedReadStart { get; private set; }

        /// <summary>
        /// The number of bytes that are currently buffered in the read stream
        /// </summary>
        public int BufferedReadLength => _readCount - _readHeadPosition - _readTailPosition;


        /// <summary>
        /// The absolute byte offset into the write stream where the first byte of buffered data is available
        /// </summary>
        public long BufferedWriteStart { get; private set; }

        /// <summary>
        /// The number of bytes that are currently buffered in the write stream
        /// </summary>
        public int BufferedWriteLength => _writeCount;

        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        public override void Flush()
        {
            if (_writeBuffers == null)
                _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_readBuffers == null)
                return _stream.Read(buffer, offset, count);

            var firstReadBuffer = _readBuffers.FirstOrDefault();

            if (_endOfReadStream)
            {
                if (firstReadBuffer == null) return 0;
            }
            else
            {
                while (!_endOfReadStream && BufferedReadLength <= _readBufferLength)
                {
                    var readBuffer = _bufferPool.Get();
                    var bytesRead = _stream.Read(readBuffer, 0, readBuffer.Length);
                    if (bytesRead == 0)
                    {
                        _endOfReadStream = true;
                        if (firstReadBuffer == null) return 0;
                    }
                    else
                    {
                        _readBuffers.Append(readBuffer);
                        _readCount += readBuffer.Length;
                        if (firstReadBuffer == null) firstReadBuffer = readBuffer;
                    }
                }
            }

            if (firstReadBuffer.Length - _readHeadPosition <= count)
            {
                firstReadBuffer = _readBuffers.PopFirst();
                _readCount -= firstReadBuffer.Length;

                var length = firstReadBuffer.Length - _readHeadPosition;
                Array.Copy(firstReadBuffer, _readHeadPosition, buffer, offset, length);
                _readHeadPosition = 0;

                _bufferPool.Reuse(firstReadBuffer);

                return length;
            }

            Array.Copy(firstReadBuffer, _readHeadPosition, buffer, offset, count);
            _readHeadPosition += count;
            return count;
        }

        /// <summary>
        /// Copies bytes from the read buffer into a buffer supplied by the caller
        /// </summary>
        /// <param name="streamOffset">The absolute position within the stream to copy from. 
        /// Must not be less than BufferedReadStart</param>
        /// <param name="count">The number of bytes to copy. Can not be more than BufferedReadLength from BufferedReadStart</param>
        /// <param name="buffer">The buffer to copy bytes into</param>
        /// <param name="offset">The offset to start copying into buffer</param>
        public void GetReadBytes(long streamOffset, int count, byte[] buffer, int offset)
        {
            GetBufferedBytes(_readBuffers, (int)(streamOffset - BufferedReadStart) + _readHeadPosition, count, buffer, offset);
        }

        /// <summary>
        /// Overwrites bytes in the read buffer. These bytes have been read from the wrapped
        /// stream already but not read from this stream yet
        /// </summary>
        /// <param name="streamOffset">The position within the stream to replace bytes</param>
        /// <param name="bytesToReplace">The number of bytes to remove from the stream starting with streamOffset. Can be 0</param>
        /// <param name="replacementBytes">The bytes to insert into the stream at streamOffset</param>
        /// <param name="replacementOffset">The offset into the replacementBytes buffer to start copying bytes from</param>
        /// <param name="replacementCount">The number of bytes to copy from replacementBytes</param>
        public void ReplaceReadBytes(long streamOffset, int bytesToReplace, byte[] replacementBytes, int replacementOffset, int replacementCount)
        {
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count < 1) return;

            if (_writeBuffers == null)
            {
                _stream.Write(buffer, offset, count);
                return;
            }

            var newBuffer = _bufferPool.Get(count);
            Array.Copy(buffer, offset, newBuffer, 0, count);

            lock (_writeLock)
            {
                _writeBuffers.Append(newBuffer);
                _writeCount += count;
            }

            while (BufferedWriteLength > _writeBufferLength) // We are buffering more bytes than we need to
            {
                var first = _writeBuffers.FirstOrDefault();
                if (first == null || _writeCount - first.Length < _writeBufferLength)
                    return; // Writing this buffer would leave us with not enough bytes in the buffer

                lock (_writeLock)
                {
                    first = _writeBuffers.PopFirst();
                    _writeCount -= first.Length;
                    BufferedWriteStart += first.Length;
                }

                _stream.Write(first, 0, first.Length);

                _bufferPool.Reuse(first);
            }
        }

        /// <summary>
        /// Copies bytes from the write buffer into a buffer supplied by the caller
        /// </summary>
        /// <param name="streamOffset">The absolute position within the stream to copy from. 
        /// Must not be less than BufferedWriteStart</param>
        /// <param name="count">The number of bytes to copy. Can not be more than BufferedWriteLength from BufferedWriteStart</param>
        /// <param name="buffer">The buffer to copy bytes into</param>
        /// <param name="offset">The offset to start copying into buffer</param>
        public void GetWrittenBytes(long streamOffset, int count, byte[] buffer, int offset)
        {
            GetBufferedBytes(_writeBuffers, (int)(streamOffset - BufferedWriteStart), count, buffer, offset);
        }

        /// <summary>
        /// Overwrites bytes in the write buffer. These bytes have been written to this stream but
        /// not yet written to the the wrapped stream
        /// </summary>
        /// <param name="streamOffset">The position within the stream to replace bytes</param>
        /// <param name="bytesToReplace">The number of bytes to remove from the stream starting with streamOffset. Can be 0</param>
        /// <param name="replacementBytes">The bytes to insert into the stream at streamOffset</param>
        /// <param name="replacementOffset">The offset into the replacementBytes buffer to start copying bytes from</param>
        /// <param name="replacementCount">The number of bytes to copy from replacementBytes</param>
        public void ReplaceWrittenBytes(long streamOffset, int bytesToReplace, byte[] replacementBytes, int replacementOffset, int replacementCount)
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException($"{GetType().Name} does not support seeking");
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException($"{GetType().Name} does not support setting the stream length");
        }

        /// <summary>
        /// Extracts bytes from the buffered data
        /// </summary>
        /// <param name="bufferList">A list of data buffers</param>
        /// <param name="start">The byte offset where reading should start</param>
        /// <param name="length">The number of bytes to read</param>
        /// <param name="buffer">The buffer to return read bytes into</param>
        /// <param name="offset">The offset within buffer to start writing bytes</param>
        private void GetBufferedBytes(
            LinkedList<byte[]> bufferList, 
            int start, int length, 
            byte[] buffer, int offset)
        {
            var bufferListElement = bufferList.FirstElement();
            var bufferedData = bufferListElement?.Data;

            while (bufferedData != null && start >= bufferedData.Length)
            {
                start -= bufferedData.Length;
                bufferListElement = bufferListElement.Next;
                bufferedData = bufferListElement?.Data;
            }

            do
            {
                if (bufferedData == null)
                    throw new Exception("Attempt to read more data than there is in the buffer");

                var bytesToCopy = length;
                if (start + bytesToCopy > bufferedData.Length)
                    bytesToCopy = bufferedData.Length - start;

                Array.Copy(bufferedData, start, buffer, offset, bytesToCopy);

                offset += bytesToCopy;
                length -= bytesToCopy;

                if (length == 0) return;

                bufferListElement = bufferListElement.Next;
                bufferedData = bufferListElement?.Data;
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
        private void ReplaceBufferedBytes(
            LinkedList<byte[]> bufferList, 
            int start, int length, 
            byte[] replacementBytes, int replacementOffset, int replacementLength)
        {
            var bufferListElement = bufferList.FirstElement();
            var bufferedData = bufferListElement?.Data;

            while (bufferedData != null && start >= bufferedData.Length)
            {
                start -= bufferedData.Length;
                bufferListElement = bufferListElement.Next;
                bufferedData = bufferListElement?.Data;
            }

            // Overwrite the overalpping bytes
            do
            {
                if (bufferedData == null)
                    throw new Exception("Attempt to overwrite past the end of the buffer");

                var bytesToCopy = length;
                if (bytesToCopy > replacementLength) bytesToCopy = replacementLength;
                if (start + bytesToCopy > bufferedData.Length) bytesToCopy = bufferedData.Length - start;

                Array.Copy(replacementBytes, replacementOffset, bufferedData, start, bytesToCopy);

                length -= bytesToCopy;
                replacementOffset += bytesToCopy;
                replacementLength -= bytesToCopy;

                if (length > 0 && replacementLength > 0)
                {
                    bufferListElement = bufferListElement.Next;
                    bufferedData = bufferListElement.Data;
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

                if (start + bytesToDelete > bufferedData.Length)
                {
                    bytesToDelete = bufferedData.Length - start;
                    if (start == 0)
                    {
                        var bufferListElementToDelete = bufferListElement;
                        bufferListElement = bufferListElement.Prior;
                        bufferList.Delete(bufferListElementToDelete);
                        _bufferPool.Reuse(bufferedData);
                    }
                    else
                    {
                        var dataToKeep = _bufferPool.Get(bufferedData.Length - bytesToDelete);
                        Array.Copy(bufferedData, 0, dataToKeep, 0, dataToKeep.Length);
                        bufferListElement.Data = dataToKeep;
                        _bufferPool.Reuse(bufferedData);
                    }
                }
                else
                {
                    if (start == 0)
                    {
                        var dataToKeep = _bufferPool.Get(bufferedData.Length - bytesToDelete);
                        Array.Copy(bufferedData, length, dataToKeep, 0, dataToKeep.Length);
                        bufferListElement.Data = dataToKeep;
                        _bufferPool.Reuse(bufferedData);
                    }
                    else
                    {
                        var dataBefore = _bufferPool.Get(start);
                        var dataAfter = _bufferPool.Get(bufferedData.Length - start - length);
                        Array.Copy(bufferedData, 0, dataBefore, 0, dataBefore.Length);
                        Array.Copy(bufferedData, start + length, dataAfter, 0, dataAfter.Length);
                        bufferListElement.Data = dataBefore;
                        bufferListElement = bufferList.InsertAfter(bufferListElement, dataAfter);
                        _bufferPool.Reuse(bufferedData);
                    }
                }

                length -= bytesToDelete;
                bufferListElement = bufferListElement?.Next ?? bufferList.FirstElementOrDefault();
                bufferedData = bufferListElement?.Data;
                start = 0;
            }

            // Append extra bytes if the replacement is longer
            if (replacementLength > 0)
            {
                var additionalData = _bufferPool.Get(replacementLength);
                Array.Copy(replacementBytes, replacementOffset, additionalData, 0, replacementLength);
                bufferList.InsertAfter(bufferListElement, additionalData);
            }
        }
    }
}