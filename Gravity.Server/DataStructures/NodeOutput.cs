using System.Threading;
using Gravity.Server.Interfaces;

namespace Gravity.Server.DataStructures
{
    internal class NodeOutput
    {
        public string Name { get; set; }
        public INode Node { get; set; }
        public bool Disabled { get; set; }

        private long _requestCounbt;
        public long RequestCount { get { return _requestCounbt; } }

        public void IncrementRequestCount()
        {
            Interlocked.Increment(ref _requestCounbt);
        }
    }
}