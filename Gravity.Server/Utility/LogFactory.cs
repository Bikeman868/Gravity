using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Gravity.Server.Interfaces;
using Microsoft.Owin;
using Urchin.Client.Interfaces;

namespace Gravity.Server.Utility
{
    internal class LogFactory: ILogFactory, IDisposable
    {
        private const string OwinEnvironmentKey = "GRAVITY_LOG";

        private IDisposable _configRegistration;
        private Configuration _configuration;
        private Func<LogType, LogLevel, bool> _filter;
        private long _nextKey;
        private LogFileWriter _logFileWriter;

        public LogFactory(
            IConfigurationStore configurationStore)
        {
            _configRegistration = configurationStore.Register(
                "/gravity/log", 
                c =>
                {
                    if ((int)c.MaxLogLevel < 1)
                    {
                        _filter = (t, l) => false;
                        c.Enabled = false;
                    }
                    else
                    {
                        var levelMask = c.LogTypes.Length == 0 
                            ? -1
                            : c.LogTypes.Aggregate(0, (m, t) => m |= (int)t);
                        _filter = (t, l) =>
                        {
                            if (l > c.MaxLogLevel) return false;
                            return ((int)t & levelMask) != 0;
                        };
                    }

                    if (c.Method == LogMethod.File)
                    {
                        if (string.IsNullOrWhiteSpace(c.Directory))
                        {
                            c.Enabled = false;
                        }
                        else
                        {
                            var oldWriter = _logFileWriter;
                            _logFileWriter = new LogFileWriter(new DirectoryInfo(c.Directory));
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
        }

        public ILog Create(IOwinContext context)
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

            if (log != null)
                context.Set(OwinEnvironmentKey, log);

            return log;
        }

        public ILog Get(IOwinContext context)
        {
            if (!_configuration.Enabled) return null;

            return context.Get<ILog>(OwinEnvironmentKey);
        }

        private class FileLog: ILog
        {
            private readonly long _key;
            private readonly Func<LogType, LogLevel, bool> _filter;
            private readonly LogFileWriter _writer;
            private readonly List<string> _logEntries = new List<string>();

            public FileLog(long key, Func<LogType, LogLevel, bool> filter, LogFileWriter writer)
            {
                _key = key;
                _filter = filter;
                _writer = writer;
            }

            public void Dispose()
            {
                _writer.WriteLog(_key, _logEntries);
            }

            public void Log(LogType type, LogLevel level, Func<string> messageFunc)
            {
                if (_filter(type, level))
                    _logEntries.Add(messageFunc());
            }
        }

        private class TraceLog : ILog
        {
            private readonly long _key;
            private readonly Func<LogType, LogLevel, bool> _filter;
            private readonly DateTime _start;

            public TraceLog(long key, Func<LogType, LogLevel, bool> filter)
            {
                _key = key;
                _filter = filter;
                _start = DateTime.UtcNow;
            }

            public void Dispose()
            {
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
            public bool Enabled { get; set; }
            public LogMethod Method { get; set; }
            public LogLevel MaxLogLevel { get; set; }
            public LogType[] LogTypes { get; set; }
            public string Directory { get; set; }

            public Configuration()
            {
                Enabled = false;
                Method = LogMethod.Trace;
                MaxLogLevel = LogLevel.Standard;
                LogTypes = new LogType[0];
                Directory = "C:\\Logs";
            }
        }

        private class LogFileWriter: IDisposable
        {
            private readonly object _lock = new object();

            public LogFileWriter(DirectoryInfo directory)
            {}

            public void Dispose()
            { }

            public void WriteLog(long key, List<string> logEntries)
            {
                lock (_lock)
                {
                    Trace.WriteLine($"LOG {key,6}");

                    foreach (var entry in logEntries)
                        Trace.WriteLine(entry);

                    Trace.WriteLine($"LOG {key,6}");
                }
            }
        }
    }
}