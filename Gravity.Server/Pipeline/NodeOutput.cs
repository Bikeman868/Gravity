using System;
using System.Threading;
using Gravity.Server.Utility;

namespace Gravity.Server.Pipeline
{
    internal class NodeOutput: IComparable
    {
        public string Name { get; set; }
        public INode Node { get; set; }
        public bool Offline { get; set; }

        public TrafficAnalytics TrafficAnalytics { get; private set; }

        private long _connectionCount;
        public long ConnectionCount { get { return _connectionCount; } }

        private long _sessionCount;
        public long SessionCount { get { return _sessionCount; } }

        public NodeOutput()
        {
            TrafficAnalytics = new TrafficAnalytics
            {
                AverageInterval = TimeSpan.FromMinutes(5)
            };
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

        public new bool Equals(object x, object y)
        {
            var a = x as NodeOutput;
            var b = y as NodeOutput;
            if (ReferenceEquals(a, null)) return ReferenceEquals(b, null);
            return string.Equals(a.Name, b?.Name, StringComparison.OrdinalIgnoreCase);
        }

        public int CompareTo(object obj)
        {
            var other = obj as NodeOutput;
            if (ReferenceEquals(other, null)) return 1;
            return Name.CompareTo(other.Name);
        }
    }
}