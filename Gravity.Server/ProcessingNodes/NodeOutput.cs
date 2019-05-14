using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using Gravity.Server.Interfaces;

namespace Gravity.Server.ProcessingNodes
{
    internal class NodeOutput
    {
        public string Name { get; set; }
        public INode Node { get; set; }
        public bool Enabled { get; set; }

        private long _requestCounbt;
        public long RequestCount { get { return _requestCounbt; } }

        public void IncrementRequestCount()
        {
            Interlocked.Increment(ref _requestCounbt);
        }
    }
}