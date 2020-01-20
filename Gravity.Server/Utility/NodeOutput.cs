using System.Threading;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;

namespace Gravity.Server.Utility
{
    internal class NodeOutput
    {
        public string Name { get; set; }
        public INode Node { get; set; }
        public bool Disabled { get; set; }

        public TrafficAnalytics TrafficAnalytics { get; private set; }

        private long _connectionCount;
        public long ConnectionCount { get { return _connectionCount; } }

        private long _sessionCount;
        public long SessionCount { get { return _sessionCount; } }

        public NodeOutput()
        {
            TrafficAnalytics = new TrafficAnalytics();
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