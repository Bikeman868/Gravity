using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using Microsoft.Owin;
using Newtonsoft.Json;
using Urchin.Client.Interfaces;

namespace Gravity.Server.Utility
{
    internal partial class LogFactory: ILogFactory, IDisposable
    {
        private IDisposable _configRegistration;
        private Configuration _configuration;
        private long _nextKey;
        private LogFileWriter _logFileWriter;
        private Func<LogType, LogLevel, bool> _filter;

        public LogFactory(
            IConfigurationStore configurationStore)
        {
            _configRegistration = configurationStore.Register(
                "/gravity/log", 
                c =>
                {
                    _filter = ConstructFilter(c.LogTypes, c.MaximumLogLevel);

                    if ((int)c.MaximumLogLevel < 1)
                        c.Enabled = false;

                    if (c.Method == LogMethod.File)
                    {
                        if (string.IsNullOrWhiteSpace(c.Directory))
                        {
                            c.Enabled = false;
                        }
                        else
                        {
                            var oldWriter = _logFileWriter;
                            _logFileWriter = new LogFileWriter(
                                new DirectoryInfo(c.Directory), 
                                "log_",
                                c.MaximumLogFileAge, 
                                c.MaximumLogFileSize,
                                false);
                            oldWriter?.Dispose();
                        }
                    }

                    _configuration = c;
                }, 
                new Configuration());
        }

        public void Dispose()
        {
            _configRegistration?.Dispose();
            _configRegistration = null;

            _logFileWriter?.Dispose();
            _logFileWriter = null;
        }

        public ILog Create(IRequestContext context)
        {
            if (!_configuration.Enabled) return null;

            ILog log = null;

            switch (_configuration.Method)
            {
                case LogMethod.File:
                    log = new FileLog(Interlocked.Increment(ref _nextKey), _filter, _logFileWriter);
                    break;

                case LogMethod.Trace:
                    log = new TraceLog(Interlocked.Increment(ref _nextKey), _filter);
                    break;
            }

            return log;
        }

        private static Func<LogType, LogLevel, bool> ConstructFilter(LogType[] logTypes, LogLevel maximumLogLevel)
        {
            if ((int)maximumLogLevel < 1)
                return (t, l) => false;

            var logTypeMask = logTypes == null || logTypes.Length == 0
                ? -1
                : logTypes.Aggregate(0, (m, t) => m |= (int)t);

            return (t, l) => l <= maximumLogLevel && ((int)t & logTypeMask) != 0;
        }

        public bool WillLog(LogType type, LogLevel level)
        {
            return _filter(type, level);
        }

        private class FileLog: ILog
        {
            private readonly long _key;
            private readonly LogFileWriter _writer;
            private readonly List<string> _logEntries = new List<string>();
            private readonly DateTime _start;

            private Func<LogType, LogLevel, bool> _filter;

            public FileLog(long key, Func<LogType, LogLevel, bool> filter, LogFileWriter writer)
            {
                _key = key;
                _filter = filter;
                _writer = writer;
                _start = DateTime.UtcNow;
            }

            public void Dispose()
            {
                Task.Run(() => _writer.WriteLog(_key, _logEntries));
            }

            public void SetFilter(LogType[] logTypes, LogLevel maximumLogLevel)
            {
                _filter = ConstructFilter(logTypes, maximumLogLevel);
            }

            public bool WillLog(LogType type, LogLevel level)
            {
                return _filter(type, level);
            }

            public void Log(LogType type, LogLevel level, Func<string> messageFunc)
            {
                if (_filter(type, level))
                {
                    var elapsed = (int)(DateTime.UtcNow - _start).TotalMilliseconds;
                    _logEntries.Add($"{elapsed,5}ms {type,-10} {messageFunc()}");
                }
            }
        }

        private class TraceLog : ILog
        {
            private readonly long _key;
            private readonly DateTime _start;

            private Func<LogType, LogLevel, bool> _filter;

            public TraceLog(long key, Func<LogType, LogLevel, bool> filter)
            {
                _key = key;
                _filter = filter;
                _start = DateTime.UtcNow;
            }

            public void Dispose()
            {
            }

            public void SetFilter(LogType[] logTypes, LogLevel maximumLogLevel)
            {
                _filter = ConstructFilter(logTypes, maximumLogLevel);
            }

            public bool WillLog(LogType type, LogLevel level)
            {
                return _filter(type, level);
            }

            public void Log(LogType type, LogLevel level, Func<string> messageFunc)
            {
                if (_filter(type, level))
                {
                    var elapsed = (int)(DateTime.UtcNow - _start).TotalMilliseconds;
                    Trace.WriteLine($"{_key,6} {elapsed,5}ms {type,-10} {messageFunc()}");
                }
            }
        }

        private enum LogMethod { Trace, File }

        private class Configuration
        {
            [JsonProperty("enabled")]
            public bool Enabled { get; set; }

            [JsonProperty("method")]
            public LogMethod Method { get; set; }

            [JsonProperty("maxLogLevel")]
            public LogLevel MaximumLogLevel { get; set; }

            [JsonProperty("logTypes")]
            public LogType[] LogTypes { get; set; }

            [JsonProperty("directory")]
            public string Directory { get; set; }

            [JsonProperty("maxLogFileAge")]
            public TimeSpan MaximumLogFileAge { get; set; }

            [JsonProperty("maxLogFileSize")]
            public long MaximumLogFileSize { get; set; }

            public Configuration()
            {
                Enabled = false;
                Method = LogMethod.Trace;
                MaximumLogLevel = LogLevel.Standard;
                LogTypes = new LogType[0];
                Directory = "C:\\Logs";
                MaximumLogFileAge = TimeSpan.FromDays(7);
                MaximumLogFileSize = 1 * 1024 * 1024;
            }
        }
    }
}