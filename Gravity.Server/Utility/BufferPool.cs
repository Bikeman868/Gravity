using Gravity.Server.Interfaces;
using System;
using OwinFramework.Utility.Containers;

namespace Gravity.Server.Utility
{
    internal class BufferPool : IBufferPool
    {
        private LinkedList<byte[]> _pool;

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
                buffer = new byte[10000];

            return buffer;
        }

        void IBufferPool.Reuse(byte[] buffer)
        {
            if (buffer != null && buffer.Length >= 1000 && buffer.Length <= 50000)
                _pool.Prepend(buffer);
        }
    }
}