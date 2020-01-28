using System;
using System.Net;
using System.Threading;

namespace Gravity.Server.Utility
{
    internal class ServerIpAddress
    {
        public IPAddress Address { get; set; }
        public TrafficAnalytics TrafficAnalytics { get; private set; }
        public bool? Healthy { get; private set; }
        public string UnhealthyReason { get; private set; }
        public int HealthCheckFailCount { get; set; }
        public int MaximumHealthCheckFailCount { get; set; }

        private int _connectionCount;
        public int ConnectionCount => _connectionCount;

        public ServerIpAddress()
        {
            TrafficAnalytics = new TrafficAnalytics
            {
                AverageInterval = TimeSpan.FromMinutes(1)
            };
        }

        public int IncrementConnectionCount()
        {
            return Interlocked.Increment(ref _connectionCount);
        }

        public int DecrementConnectionCount()
        {
            return Interlocked.Decrement(ref _connectionCount);
        }

        public void SetHealthy()
        {
            Healthy = true;
            HealthCheckFailCount = 0;
        }

        public void SetUnhealthy(string reason)
        {
            if (HealthCheckFailCount++ >= MaximumHealthCheckFailCount)
            {
                UnhealthyReason = reason;
                Healthy = false;
            }
        }
    }
}