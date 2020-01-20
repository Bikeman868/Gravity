using System.Runtime.InteropServices;

namespace Gravity.Server.Utility
{
    /// <summary>
    /// Provides very high performance time measurement.
    /// Accurate to sub-nanosecond resolution.
    /// Works like a stop watch, can be started and stopped many times and it accumulates elapsed time while running.
    /// Can query elapsed time after it has been stopped, or while it is running.
    /// </summary>
    internal class Timer
    {
        #region Static stuff for access to very high precision timing

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long performanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long frequency);

        private static readonly long frequency;

        static Timer()
        {
            QueryPerformanceFrequency(out frequency);
        }

        public static long TimeNow
        {
            get
            {
                long startTime;
                QueryPerformanceCounter(out startTime);
                return startTime;
            }
        }

        #endregion

        public static double TicksToSeconds(long ticks)
        {
            return ((double)ticks) / frequency;
        }

        public static long SecondsToTicks(double seconds)
        {
            return (long)(seconds * frequency);
        }

        public static double TicksToMilliseconds(long ticks)
        {
            return 1000d * ticks / frequency;
        }

        public static double TicksToMicroseconds(long ticks)
        {
            return 1000000d * ticks / frequency;
        }

        public static double TicksToNanoseconds(long ticks)
        {
            return 1000000000d * ticks / frequency;
        }

        private long _elapsedTime;
        private bool _running;

        public long ElapsedTicks { get { return _running ? _elapsedTime + (TimeNow - StartTicks) : _elapsedTime; } }
        public long StartTicks { get; private set; }

        public double ElapsedSeconds { get { return TicksToSeconds(ElapsedTicks); } }
        public double ElapsedMilliSeconds { get { return TicksToMilliseconds(ElapsedTicks); } }
        public double ElapsedMicroSeconds { get { return TicksToMicroseconds(ElapsedTicks); } }
        public double ElapsedNanoSeconds { get { return TicksToNanoseconds(ElapsedTicks); } }

        public Timer Initialize(long startTicks)
        {
            StartTicks = startTicks;
            _running = true;
            return this;
        }

        public Timer Start()
        {
            _running = true;
            StartTicks = TimeNow;
            return this;
        }

        public Timer Stop()
        {
            if (_running)
            {
                _elapsedTime += TimeNow - StartTicks;
                _running = false;
            }
            return this;
        }
    }
}
