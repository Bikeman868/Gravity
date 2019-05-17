using System.Net;
using System.Threading;

namespace Gravity.Server.DataStructures
{
    internal class ServerIpAddress
    {
        public IPAddress Address { get; set; }

        private long _requestCount;
        public long RequestCount { get { return _requestCount; } }

        private long _connectionCount;
        public long ConnectionCount { get { return _connectionCount; } }

        private bool _healthy;
        public bool Healthy { get { return _healthy; } }

        private string _unhealthyReason;
        public string UnhealthyReason { get { return _unhealthyReason; } }

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

        public void SetHealthy()
        {
            _healthy = true;
        }

        public void SetUnhealthy(string reason)
        {
            _unhealthyReason = reason;
            _healthy = false;
        }
    }
}