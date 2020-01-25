using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Gravity.Server.Utility
{
    internal class LogFileWriter : IDisposable
    {
        private readonly object _lock = new object();

        private readonly DirectoryInfo _directory;
        private readonly TimeSpan _maximumLogFileAge;
        private readonly long _maximumLogFileSize;
        private readonly string _fileNamePrefix;
        private readonly bool _bare;

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
            string fileNamePrefix,
            TimeSpan maximumLogFileAge,
            long maximumLogFileSize,
            bool bare)
        {
            _directory = directory;
            _maximumLogFileAge = maximumLogFileAge;
            _maximumLogFileSize = maximumLogFileSize;
            _fileNamePrefix = fileNamePrefix;
            _bare = bare;

            CreateFile();
        }

        ~LogFileWriter()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
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
                        Trace.WriteLine($"{_fileNamePrefix} {key,6} {logEntries[0]}");
                    }
                    else
                    {
                        Trace.WriteLine($"{_fileNamePrefix} {key,6}");

                        foreach (var entry in logEntries)
                            Trace.WriteLine("  " + entry);

                        Trace.WriteLine($"{_fileNamePrefix} {key,6}");
                    }
                    CreateFile();
                }
                else
                {
                    if (logEntries.Count == 1)
                    {
                        if (_bare)
                            _fileWriter.WriteLine(logEntries[0]);
                        else
                            _fileWriter.WriteLine($"{key,06} {logEntries[0]}");
                    }
                    else
                    {
                        if (_bare)
                        {
                            foreach (var entry in logEntries)
                                _fileWriter.WriteLine(entry);
                        }
                        else
                        {
                            _fileWriter.WriteLine(key.ToString("d08"));

                            foreach (var entry in logEntries)
                                _fileWriter.WriteLine("  " + entry);

                            _fileWriter.WriteLine(key.ToString("d08"));
                        }
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

                _fileInfo = new FileInfo(_directory.FullName + "\\" + _fileNamePrefix + DateTime.UtcNow.Ticks.ToString("d020") + ".txt");
                _fileWriter = new StreamWriter(File.Open(_fileInfo.FullName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read), Encoding.UTF8);
                _fileWriter.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ssK"));
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

            foreach (var file in _directory.GetFiles(_fileNamePrefix + "*.txt"))
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