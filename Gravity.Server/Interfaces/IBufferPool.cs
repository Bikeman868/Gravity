using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gravity.Server.Interfaces
{
    /// <summary>
    /// Defines a mechanism for reusing byte arrays to avoid excessive garbage collection thrash
    /// </summary>
    internal interface IBufferPool
    {
        /// <summary>
        /// Gets a byte array of the specified size
        /// </summary>
        byte[] Get(int? size = null);

        /// <summary>
        /// Puts a byte array into the pool for reuse. Make sure not to
        /// access this byte array after passing to this method
        /// </summary>
        void Reuse(byte[] buffer);
    }
}