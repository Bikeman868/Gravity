using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Gravity.Server.Utility
{
    public class TrafficAnalytics
    {
        private readonly object _lock;
        private List<IntervalStats> _intervals;
        private List<TrafficAnalyticInfo> _requests;

        private long _lifetimeRequests;
        private double _requestsPerMinute;
        private long _ticksPerRequest;
        private DefaultDictionary<string, double> _methodsPerMinute;
        private DefaultDictionary<ushort, double> _statusCodesPerMinute;


        private TimeSpan _averageInterval;
        private long _intervalTicks;

        public TimeSpan AverageInterval
        {
            get => _averageInterval;
            set
            {
                _averageInterval = value;
                _intervalTicks = Timer.SecondsToTicks(value.TotalSeconds);
            }
        }

        public IDictionary<ushort, double> StatusCodesPerMinute => _statusCodesPerMinute;
        public IDictionary<string, double> MethodsPerMinute => _methodsPerMinute;
        public long LifetimeRequestCount => Interlocked.Read(ref _lifetimeRequests);
        public double RequestsPerMinute => _requestsPerMinute; 
        public TimeSpan RequestTime => TimeSpan.FromMilliseconds(Timer.TicksToMilliseconds(_ticksPerRequest));

        public TrafficAnalytics()
        {
            _requests = new List<TrafficAnalyticInfo>();
            _lock = new object();
            _intervals = new List<IntervalStats>();
            AverageInterval = TimeSpan.FromSeconds(60);
            _methodsPerMinute = new DefaultDictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            _statusCodesPerMinute = new DefaultDictionary<ushort, double>(NumberComparer.UnsignedShort);
        }

    public TrafficAnalyticInfo BeginRequest()
        {
            return new TrafficAnalyticInfo
            {
                StartTicks = Timer.TimeNow
            };
        }

        public void EndRequest(TrafficAnalyticInfo info)
        {
            info.EndTicks = Timer.TimeNow;
            lock (_lock) _requests.Add(info);

            Interlocked.Increment(ref _lifetimeRequests);
        }

        public void Recalculate()
        {
            var requests = _requests;
            _requests = new List<TrafficAnalyticInfo>();

            var currentInterval = new IntervalStats
            {
                MethodCounts = new DefaultDictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                StatusCounts = new DefaultDictionary<ushort, int>(NumberComparer.UnsignedShort)
            };
            
            lock (_lock)
            {
                currentInterval.RequestCount = requests.Count;
                if (currentInterval.RequestCount > 0)
                {
                    currentInterval.StartTicks = requests[0].StartTicks;
                    currentInterval.EndTicks = requests[requests.Count - 1].EndTicks;

                    foreach (var request in requests)
                    {
                        currentInterval.ElapsedSum += request.EndTicks - request.StartTicks;

                        if (request.StatusCode != 0)
                            currentInterval.StatusCounts[request.StatusCode] = currentInterval.StatusCounts[request.StatusCode] + 1;

                        if (!string.IsNullOrEmpty(request.Method))
                            currentInterval.MethodCounts[request.Method] = currentInterval.MethodCounts[request.Method] + 1;
                    }
                }
            }

            var oldestIntervalTicks = Timer.TimeNow - _intervalTicks;
            _intervals = _intervals.Where(i => i.EndTicks > oldestIntervalTicks).ToList();
            _intervals.Add(currentInterval);

            var requestCountSum = _intervals.Sum(i => i.RequestCount);
            var elapsedTicksSum =  _intervals.Sum(i => i.ElapsedSum);

            var firstInterval = _intervals.FirstOrDefault(i => i.RequestCount > 0);
            var elapsedTicks = firstInterval == null ? 0 : Timer.TimeNow - firstInterval.StartTicks;
            var elapsedSeconds = Timer.TicksToSeconds(elapsedTicks);

            var methodsPerMinute = new DefaultDictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var statusCodesPerMinute = new DefaultDictionary<ushort, double>(NumberComparer.UnsignedShort);

            if (elapsedSeconds > 0)
            {
                foreach (var interval in _intervals)
                {
                    var intervalMethods = interval.MethodCounts.Keys.ToList();
                    foreach (var method in intervalMethods)
                        methodsPerMinute[method] = methodsPerMinute[method] + interval.MethodCounts[method];

                    var intervalStatuses = interval.StatusCounts.Keys.ToList();
                    foreach (var status in intervalStatuses)
                        statusCodesPerMinute[status] = statusCodesPerMinute[status] + interval.StatusCounts[status];
                }

                var methods = methodsPerMinute.Keys.ToList();
                foreach (var method in methods)
                    methodsPerMinute[method] = methodsPerMinute[method] * 60 / elapsedSeconds;

                var statusCodes = statusCodesPerMinute.Keys.ToList();
                foreach (var status in statusCodes)
                    statusCodesPerMinute[status] = statusCodesPerMinute[status] * 60 / elapsedSeconds;
            }

            _ticksPerRequest = requestCountSum > 0 ? elapsedTicksSum / requestCountSum : 0L;
            _requestsPerMinute = elapsedSeconds > 0 ? 60 * requestCountSum / elapsedSeconds : 0;
            _methodsPerMinute = methodsPerMinute;
            _statusCodesPerMinute = statusCodesPerMinute;
        }

        private class IntervalStats
        {
            public long ElapsedSum;
            public long StartTicks;
            public long EndTicks;
            public int RequestCount;
            public IDictionary<string, int> MethodCounts;
            public IDictionary<ushort, int> StatusCounts;
        }
    }
}