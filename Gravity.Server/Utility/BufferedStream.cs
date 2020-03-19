using Gravity.Server.Interfaces;
using OwinFramework.Utility.Containers;
using System;
using System.IO;

namespace Gravity.Server.Utility
{
    /// <summary>
    /// Objects of this type can wrap a stream to provide random 
    /// access to some small part of the stream so that the stream content
    /// can be transformed in some way as the data flows through the
    /// stream without keeping the whole stream in memory.
    /// 
    /// Note that objects of this type are not thread-safe and can not
    /// be accessed by two threads at the same time. In general streams need
    /// to be processed sequentially so making this thread-safe is pointless
    /// </summary>
    internal class BufferedStream : Stream
    {
        private readonly Stream _stream;
        private readonly IBufferPool _bufferPool;
        private readonly int _readBytesToKeepInMemory;
        private readonly int _writeBytesToKeepInMemory;
        private readonly FlexibleByteArray _readBuffer;
        private readonly FlexibleByteArray _writeBuffer;
        private Action<BufferedStream> _onBytesRead;
        private Action<BufferedStream> _onBytesWritten;

        /// <summary>
        /// True after we read 0 bytes from the stream
        /// </summary>
        private bool _endOfReadStream;

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
            Action<BufferedStream> onBytesRead,
            int writeBufferLength,
            Action<BufferedStream> onBytesWritten)
        {
            _stream = stream;
            _bufferPool = bufferPool;
            _readBytesToKeepInMemory = readBufferLength;
            _onBytesRead = onBytesRead;
            _writeBytesToKeepInMemory = writeBufferLength;
            _onBytesWritten = onBytesWritten;

            if (readBufferLength > 0)
            {
                if (onBytesRead == null)
                    throw new ArgumentException("There is no point to buffer reads if there is no processing action");
                _readBuffer = new FlexibleByteArray(bufferPool);
            }

            if (writeBufferLength > 0)
            {
                if (onBytesWritten == null)
                    throw new ArgumentException("There is no point to buffer writes if there is no processing action");
                _writeBuffer = new FlexibleByteArray(bufferPool);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Close();
                _stream.Dispose();

                _readBuffer?.Dispose();
                _writeBuffer?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Note that the implementation of Close() in the base Stream
        /// class calls the dispose method which disposes of the wrapped stream.
        /// This implementation does not call the base.Close() method, so you
        /// must call Dispose() or have a using statement to close the wrapped
        /// stream.
        /// </summary>
        public override void Close()
        {
            if (_writeBuffer != null)
            {
                _onBytesWritten(this);

                while (_writeBuffer.Length > 0)
                {
                    _writeBuffer.GetReadBuffer(0, out var buffer, out var offset, out var count);
                    _stream.Write(buffer, offset, count);
                    _writeBuffer.Delete(0, count);
                }

                _stream.Flush();
            }
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
        public long BufferedReadLength => _readBuffer.Length;

        /// <summary>
        /// The absolute byte offset into the write stream where the first byte of buffered data is available
        /// </summary>
        public long BufferedWriteStart { get; private set; }

        /// <summary>
        /// The number of bytes that are currently buffered in the write stream
        /// </summary>
        public long BufferedWriteLength => _writeBuffer.Length;

        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        public override void Flush()
        {
            if (_writeBuffer == null)
                _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_readBuffer == null)
                return _stream.Read(buffer, offset, count);

            while (!_endOfReadStream && _readBuffer.Length <= _readBytesToKeepInMemory)
            {
                var updateFunc = _readBuffer.GetAppendBuffer(_readBytesToKeepInMemory, out var readBuffer, out var readBufferStart, out var readBufferCount);
                var bytesRead = _stream.Read(readBuffer, readBufferStart, readBufferCount);

                if (bytesRead == 0)
                    _endOfReadStream = true;
                else
                    updateFunc(bytesRead);
            }
            
            _onBytesRead(this);

            var bytesAvailable = (int)(_endOfReadStream ? _readBuffer.Length : _readBuffer.Length - _readBytesToKeepInMemory);
            if (bytesAvailable > count) bytesAvailable = count;

            if (bytesAvailable > 0)
            {
                _readBuffer.GetReadBuffer(0, out var readBuffer, out var readBufferStart, out var readBufferCount);
                if (readBufferCount < bytesAvailable) bytesAvailable = readBufferCount;

                Array.Copy(readBuffer, readBufferStart, buffer, offset, bytesAvailable);

                _readBuffer.Delete(0, bytesAvailable);
                BufferedReadStart += bytesAvailable;
            }

            return bytesAvailable;
        }

        /// <summary>
        /// Copies bytes from the read buffer into a buffer supplied by the caller. These
        /// bytes have been read from the wrapped stream already but not read from the
        /// buffered stream.
        /// </summary>
        /// <param name="streamOffset">The absolute position within the stream to copy from. 
        /// Must not be less than BufferedReadStart</param>
        /// <param name="count">The number of bytes to copy. Can not be more than BufferedReadLength from BufferedReadStart</param>
        /// <param name="buffer">The buffer to copy bytes into</param>
        /// <param name="offset">The offset to start copying into buffer</param>
        public void GetUnreadBytes(long streamOffset, int count, byte[] buffer, int offset)
        {
            GetBufferedBytes(_readBuffer, (int)(streamOffset - BufferedReadStart), count, buffer, offset);
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
        public void ReplaceUnreadBytes(long streamOffset, int bytesToReplace, byte[] replacementBytes, int replacementOffset, int replacementCount)
        {
            _readBuffer.Replace(
                streamOffset - BufferedReadStart,
                bytesToReplace,
                replacementBytes,
                replacementOffset,
                replacementCount);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count < 1) return;

            if (_writeBuffer == null)
            {
                _stream.Write(buffer, offset, count);
                return;
            }

            _writeBuffer.Append(buffer, offset, count);

            if (_writeBuffer.Length > _writeBytesToKeepInMemory)
                _onBytesWritten(this);

            while (_writeBuffer.Length > _writeBytesToKeepInMemory)
            {
                _writeBuffer.GetReadBuffer(0, out var writeBuffer, out var writeBufferOffset, out var writeBufferCount);

                var bytesToWrite = (int)(_writeBuffer.Length - _writeBytesToKeepInMemory);
                if (writeBufferCount < bytesToWrite) bytesToWrite = writeBufferCount;

                _stream.Write(writeBuffer, writeBufferOffset, bytesToWrite);

                _writeBuffer.Delete(0, bytesToWrite);
                BufferedWriteStart += bytesToWrite;
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
            GetBufferedBytes(_writeBuffer, (int)(streamOffset - BufferedWriteStart), count, buffer, offset);
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
            _writeBuffer.Replace(
                streamOffset - BufferedWriteStart,
                bytesToReplace,
                replacementBytes, 
                replacementOffset, 
                replacementCount);
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
        /// <param name="byteArray">A flexible byte array</param>
        /// <param name="start">The byte offset where reading should start</param>
        /// <param name="length">The number of bytes to read</param>
        /// <param name="buffer">The buffer to return read bytes into</param>
        /// <param name="offset">The offset within buffer to start writing bytes</param>
        private void GetBufferedBytes(
            FlexibleByteArray byteArray,
            int start, 
            int length,
            byte[] buffer, 
            int offset)
        {
            if (length < 1) return;

            while (start < byteArray.Length)
            {
                byteArray.GetReadBuffer(start, out var readBuffer, out var readBufferOffset, out var readCount);

                var bytesToCopy = readCount > length ? length : readCount;
                Array.Copy(readBuffer, readBufferOffset, buffer, offset, bytesToCopy);

                start += bytesToCopy;
                length -= bytesToCopy;
                offset += bytesToCopy;
            }
        }
    }
}