using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace Gravity.Server.Utility
{
    public class TrafficAnalytics
    {
        private readonly List<IntervalStats> _intervals;
        private readonly object _lock;

        private long _lifetimeRequests;
        private double _requestsPerMinute;
        private long _ticksPerRequest;
        private List<Tuple<long, long>> _requests;

        public long LifetimeRequestCount
        {
            get { return Interlocked.Read(ref _lifetimeRequests); }
        }

        public double RequestsPerMinute
        {
            get { return _requestsPerMinute; }
        }

        public TimeSpan RequestTime
        {
            get { return TimeSpan.FromMilliseconds(Timer.TicksToMilliseconds(_ticksPerRequest)); }
        }

        public TrafficAnalytics()
        {
            _requests = new List<Tuple<long, long>>();
            _lock = new object();
            _intervals = new List<IntervalStats>();
        }

        public long BeginRequest()
        {
            return Timer.TimeNow;
        }

        public void EndRequest(long startTime)
        {
            Interlocked.Increment(ref _lifetimeRequests);
            lock (_lock) _requests.Add(new Tuple<long, long>(startTime, Timer.TimeNow));
        }

        public void Recalculate()
        {
            var requests = _requests;
            _requests = new List<Tuple<long, long>>();

            var currentInterval = new IntervalStats();
            
            lock (_lock)
            {
                currentInterval.RequestCount = requests.Count;
                if (currentInterval.RequestCount > 0)
                {
                    currentInterval.StartTicks = requests[0].Item1;
                    currentInterval.EndTicks = requests[currentInterval.RequestCount - 1].Item2;
                    currentInterval.ElapsedSum = requests.Sum(request => request.Item2 - request.Item1);
                }
            }

            _intervals.Add(currentInterval);
            if (_intervals.Count > 10) _intervals.RemoveAt(0);

            var requestCount = _intervals.Sum(i => i.RequestCount);
            var tickCount =  _intervals.Sum(i => i.ElapsedSum);
            var elapsedTicks = _intervals[_intervals.Count - 1].EndTicks - _intervals[0].StartTicks;
            var elapsedSeconds = Timer.TicksToSeconds(elapsedTicks);

            Interlocked.Exchange(ref _ticksPerRequest, requestCount > 0 ? tickCount / requestCount : 0L);
            _requestsPerMinute = elapsedSeconds > 0 ? 60 * requestCount / elapsedSeconds : 0;
        }

        private class IntervalStats
        {
            public long ElapsedSum;
            public long StartTicks;
            public long EndTicks;
            public int RequestCount;
        }
    }
}