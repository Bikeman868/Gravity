using Gravity.Server.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gravity.Server.Utility
{
    internal class BufferPool : IBufferPool
    {
        byte[] IBufferPool.Get(int size)
        {
            return new byte[size];
        }

        void IBufferPool.Reuse(byte[] buffer)
        {
            // In this version let the garbage collector handle it
        }
    }
}