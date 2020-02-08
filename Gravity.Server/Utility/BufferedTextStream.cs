using Gravity.Server.Interfaces;
using OwinFramework.Pages.Core.Collections;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Web;

namespace Gravity.Server.Utility
{
    /// <summary>
    /// Objects of this type can wrap a stream to provide random 
    /// access to some part of the stream so that the stream content
    /// can be transformed in some way as the data flows through the
    /// stream without keeping the whole stream in memory
    /// </summary>
    internal class BufferedTextStream : Stream
    {
        private readonly Stream _stream;
        private readonly IBufferPool _bufferPool;
        private readonly int _readBufferLength;
        private readonly int _writeBufferLength;
        private LinkedList<byte[]> _readBuffers;
        private LinkedList<byte[]> _writeBuffers;

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
        private int _readPosition;

        /// <summary>
        /// Constructs a wrapper around a source stream
        /// </summary>
        /// <param name="stream">The underlying stream to wrap</param>
        /// <param name="bufferPool">Pools and reused byte arrays</param>
        /// <param name="readBufferLength">The number of bytes read from the stream that must be kept in memory for incoming stream processing</param>
        /// <param name="writeBufferLength">The number of bytes to hold back from being written to the stream until the outgoing stream has been processed</param>
        public BufferedTextStream(
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

            if (_endOfReadStream)
            {
                var first = _readBuffers.PopFirst();
                if (first == null) return 0;

                if (first.Length <= count)
                {
                    Array.Copy(first, 0, buffer, offset, first.Length);
                    _readCount -= first.Length;
                    return first.Length;
                }

                _readBuffers.Prepend(first);
                _readPosition = count;
                _readCount -= count;
            }

            while (_readCount <= _readBufferLength)
            {

                var readBuffer = _bufferPool.Get();
                var bytesRead = _stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    _endOfReadStream = true;
                }
            }
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

            _writeBuffers.Append(newBuffer);
            _writeCount += count;

            while (_writeCount > _writeBufferLength)
            {
                var first = _writeBuffers.FirstOrDefault();
                if (first == null || _writeCount - first.Length < _writeBufferLength)
                    return;

                first = _writeBuffers.PopFirst();
                _writeCount -= first.Length;
                _stream.Write(first, 0, first.Length);

                _bufferPool.Reuse(first);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException($"{GetType().Name} does not support seeking");
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException($"{GetType().Name} does not support setting the stream length");
        }

    }
}