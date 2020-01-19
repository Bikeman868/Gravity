using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gravity.Server.Interfaces
{
    internal interface IBufferPool
    {
        byte[] Get(int size = 50000);
        void Reuse(byte[] buffer);
    }
}