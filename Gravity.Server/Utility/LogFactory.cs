using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using Microsoft.Owin;
using Urchin.Client.Interfaces;

namespace Gravity.Server.Utility
{
    internal class LogFactory: ILogFactory, IDisposable
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
                    if ((int)c.MaxLogLevel < 1)
                    {
                        _filter = (t, l) => false;
                        c.Enabled = false;
                    }
                    else
                    {
                        var logTypeMask = c.LogTypes.Length == 0 
                            ? -1
                            : c.LogTypes.Aggregate(0, (m, t) => m |= (int)t);
                        _filter = (t, l) => l <= c.MaxLogLevel && ((int)t & logTypeMask) != 0;
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
                            _logFileWriter = new LogFileWriter(new DirectoryInfo(c.Directory), c.MaximumLogFileAge, c.MaximumLogFileSize);
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

        public bool WillLog(LogType type, LogLevel level)
        {
            return _filter(type, level);
        }

        private class FileLog: ILog
        {
            private readonly long _key;
            private readonly Func<LogType, LogLevel, bool> _filter;
            private readonly LogFileWriter _writer;
            private readonly List<string> _logEntries = new List<string>();
            private readonly DateTime _start;

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
            public bool Enabled { get; set; }
            public LogMethod Method { get; set; }
            public LogLevel MaxLogLevel { get; set; }
            public LogType[] LogTypes { get; set; }
            public string Directory { get; set; }
            public TimeSpan MaximumLogFileAge { get; set; }
            public long MaximumLogFileSize { get; set; }

            public Configuration()
            {
                Enabled = false;
                Method = LogMethod.Trace;
                MaxLogLevel = LogLevel.Standard;
                LogTypes = new LogType[0];
                Directory = "C:\\Logs";
                MaximumLogFileAge = TimeSpan.FromDays(7);
                MaximumLogFileSize = 1 * 1024 * 1024;
            }
        }

        private class LogFileWriter: IDisposable
        {
            private readonly object _lock = new object();

            private readonly DirectoryInfo _directory;
            private TimeSpan _maximumLogFileAge;
            private long _maximumLogFileSize;

            private FileInfo _fileInfo;
            private TextWriter _fileWriter;

            private bool CanWrite
            {
                get
                {
                    if (_fileInfo == null || !_fileInfo.Exists) return false;
                    _fileInfo.Refresh();
                    return _fileInfo.Length < _maximumLogFileSize;
                }
            }

            public LogFileWriter(
                DirectoryInfo directory,
                TimeSpan maximumLogFileAge,
                long maximumLogFileSize)
            {
                _directory = directory;
                _maximumLogFileAge = maximumLogFileAge;
                _maximumLogFileSize = maximumLogFileSize;

                CreateFile();
            }

            public void Dispose()
            {
                CloseFile();
            }

            public void WriteLog(long key, List<string> logEntries)
            {
                if (logEntries == null || logEntries.Count == 0)
                    return;

                lock (_lock)
                {
                    if (_fileWriter == null)
                    {
                        if (logEntries.Count == 1)
                        {
                            Trace.WriteLine($"LOG {key,6} {logEntries[0]}");
                        }
                        else
                        {
                            Trace.WriteLine($"LOG {key,6}");

                            foreach (var entry in logEntries)
                                Trace.WriteLine("  " + entry);

                            Trace.WriteLine($"LOG {key,6}");
                        }
                        CreateFile();
                    }
                    else
                    {
                        if (logEntries.Count == 1)
                        {
                            _fileWriter.WriteLine($"{key,06} {logEntries[0]}");
                        }
                        else
                        {
                            _fileWriter.WriteLine(key.ToString("d08"));

                            foreach (var entry in logEntries)
                                _fileWriter.WriteLine("  " + entry);

                            _fileWriter.WriteLine(key.ToString("d08"));
                        }
                        _fileWriter.Flush();

                        if (!CanWrite)
                        {
                            try
                            {
                                CloseFile();
                            }
                            finally
                            {
                                CreateFile();
                            }
                        }
                    }
                }
            }

            private void CreateFile()
            {
                if (_directory == null) return;

                try
                {
                    if (!_directory.Exists) _directory.Create();

                    _fileInfo = new FileInfo(_directory.FullName + "\\log_" + DateTime.UtcNow.Ticks.ToString("d020") + ".txt");
                    _fileWriter = new StreamWriter(_fileInfo.Create(), Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    _fileInfo = null;
                    Trace.WriteLine("FILE LOGGER EXCEPTION " + ex.Message);
                }
            }

            private void CloseFile()
            {
                _fileInfo = null;
                if (_fileWriter != null)
                {
                    try
                    {
                        _fileWriter.Close();
                        DeleteExpired();
                    }
                    finally
                    {
                        _fileWriter = null;
                    }
                }
            }

            private void DeleteExpired()
            {
                var oldest = DateTime.UtcNow - _maximumLogFileAge;

                foreach (var file in _directory.GetFiles("log_*.txt"))
                {
                    try
                    {
                        if (file.LastWriteTimeUtc < oldest)
                            file.Delete();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"ERROR DELETING LOG FILE '{file.FullName}' {ex.Message}");
                    }
                }
            }
        }
    }
}