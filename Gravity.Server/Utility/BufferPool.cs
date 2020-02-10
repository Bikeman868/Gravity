using Gravity.Server.Interfaces;
using System;
using OwinFramework.Utility.Containers;

namespace Gravity.Server.Utility
{
    internal class BufferPool : IBufferPool
    {
        private LinkedList<byte[]> _pool;
        private const int _minimumLength = 1024;
        private const int _defaultLength = 32768;
        private const int _maximumLength = 65536;

        public BufferPool()
        {
            _pool = new LinkedList<byte[]>();
        }

        byte[] IBufferPool.Get(int? size)
        {
            if (size.HasValue)
                return new byte[size.Value];

            var buffer = _pool.PopFirst();

            if (buffer == null)
                buffer = new byte[_defaultLength];

            return buffer;
        }

        byte[] IBufferPool.GetAtLeast(int minimumSize)
        {
            if (minimumSize < _minimumLength) minimumSize = _minimumLength;

            while (true)
            {
                var buffer = _pool.PopFirst();

                if (buffer == null)
                    return new byte[ToPowerOfTwo(minimumSize)];

                if (buffer.Length >= minimumSize)
                    return buffer;
            }
        }

        void IBufferPool.Reuse(byte[] buffer)
        {
            if (buffer != null && buffer.Length >= _minimumLength && buffer.Length <= _maximumLength)
                _pool.PushFirst(buffer);
        }

        private int ToPowerOfTwo(int x)
        {
            if (x < 0) { return 0; }
            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }    
    }
}