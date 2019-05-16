using System.Threading;
using Gravity.Server.Interfaces;

namespace Gravity.Server.DataStructures
{
    internal class NodeOutput
    {
        public string Name { get; set; }
        public INode Node { get; set; }
        public bool Disabled { get; set; }

        private long _requestCount;
        public long RequestCount { get { return _requestCount; } }

        private long _connectionCount;
        public long ConnectionCount { get { return _connectionCount; } }

        private long _sessionCount;
        public long SessionCount { get { return _sessionCount; } }

        public void IncrementRequestCount()
        {
            Interlocked.Increment(ref _requestCount);
        }

        public void IncrementConnectionCount()
        {
            Interlocked.Increment(ref _connectionCount);
        }

        public void DecrementConnectionCount()
        {
            Interlocked.Decrement(ref _connectionCount);
        }

        public void IncrementSessionCount()
        {
            Interlocked.Increment(ref _sessionCount);
        }

        public void DecrementSessionCount()
        {
            Interlocked.Decrement(ref _sessionCount);
        }
    }
}