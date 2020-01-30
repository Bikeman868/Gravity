using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using OwinFramework.Pages.Core.Collections;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gravity.Server.ProcessingNodes.Server
{
    internal class RequestStreamException : Exception
    {
        public RequestStreamException(string message)
            : base(message) { }

        public RequestStreamException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    internal class RequestStreamTimeoutException : RequestStreamException
    {
        public RequestStreamTimeoutException(string message)
            : base(message) { }
    }

    /// <summary>
    /// Provides a wrapper around the incoming and outgoing data stream that
    /// allows a small pool of threads to manage a large number of concurrent
    /// TCP connections. Note that the incoming and outgoing direction each read
    /// from an input stream and write to an output stream so there are 4 streams
    /// altogether.
    /// </summary>
    internal class RequestStream: IDisposable
    {
        private readonly IBufferPool _bufferPool;
        private readonly AutoResetEvent _event;
        private readonly TaskCompletionSource<bool> _taskCompletionSource;
        private readonly LinkedList<Buffer> _incomingBuffers;
        private readonly LinkedList<Buffer> _outgoingBuffers;

        private Connection _connection;
        private IRequestContext _context;
        private TimeSpan _responseTimeout;
        private int _readTimeoutMs;

        private Endpoint _incomingRead;
        private Endpoint _incomingWrite;
        private HttpReceiveEndpoint _outgoingRead;
        private Endpoint _outgoingWrite;

        public Task<bool> Task => _taskCompletionSource.Task;

        public RequestStream(            
            IBufferPool bufferPool)
        {
            _bufferPool = bufferPool;
            _event = new AutoResetEvent(true);
            _incomingBuffers = new LinkedList<Buffer>();
            _outgoingBuffers = new LinkedList<Buffer>();
            _taskCompletionSource = new TaskCompletionSource<bool>();
        }

        public RequestStream Start(
            Connection connection,
            IRequestContext context, 
            TimeSpan responseTimeout, 
            int readTimeoutMs)
        {
            _connection = connection;
            _context = context;
            _readTimeoutMs = readTimeoutMs;

            var outgoingCanHaveContent =
                !string.Equals(context.Incoming.Method, "HEAD", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(context.Incoming.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase);

            _incomingRead = new Endpoint(context.Log, "Incoming read", TimeSpan.FromSeconds(5));
            _incomingWrite = new Endpoint(context.Log, "Incoming write", TimeSpan.FromSeconds(5));
            _outgoingRead = new HttpReceiveEndpoint(context.Log, "Outgoing read", responseTimeout, outgoingCanHaveContent);
            _outgoingWrite = new Endpoint(context.Log, "Outgoing write", TimeSpan.FromSeconds(5));

            _incomingRead.NextStep = IncomingFirstStep;

            _connection.BeginTransaction(context.Log);

            return this;
        }

        public void Dispose()
        {
            _event.Dispose();
        }

        /// <summary>
        /// Checks for anything to do on an this stream returning immediately
        /// so that the thread can check other active connections.
        /// </summary>
        /// <returns>Returns true if there are more steps to complete</returns>
        public bool NextStep()
        {
            // Return immediately if another thread is already servicing this request
            if (!_event.WaitOne(0)) return true;

            try
            {
                if (_incomingRead.NextStep == null && 
                    _incomingWrite.NextStep == null &&
                    _outgoingRead.NextStep == null &&
                    _outgoingWrite.NextStep == null)
                    return false;

                if (_incomingRead.NextStep != null) _incomingRead.NextStep();
                if (_outgoingRead.NextStep != null) _outgoingRead.NextStep();
                if (_incomingWrite.NextStep != null) _incomingWrite.NextStep();
                if (_outgoingWrite.NextStep != null) _outgoingWrite.NextStep();

                if (_incomingRead.NextStep == null && 
                    _incomingWrite.NextStep == null &&
                    _outgoingRead.NextStep == null &&
                    _outgoingWrite.NextStep == null)
                {
                    _taskCompletionSource.SetResult(true);
                    return false;
                }
            }
            catch
            {
                _incomingRead.Clear();
                _incomingWrite.Clear();
                _outgoingRead.Clear();
                _outgoingWrite.Clear();

                _connection.EndTransaction(_context.Log, false);

                _taskCompletionSource.SetResult(false);

                return false;
            }
            finally
            {
                // Allow other threads to service this connection
                _event.Set();
            }

            return true;
        }

        #region Incoming steps

        private void IncomingFirstStep()
        {
            _connection.ReceiveTimeoutMs = (int)_responseTimeout.TotalMilliseconds;

            var incoming = _context.Incoming;
            incoming.SendHeaders(_context);

            var head = new StringBuilder();

            head.Append(incoming.Method);
            head.Append(' ');
            //head.Append(incoming.Scheme == Scheme.Https ? "https" : " http");
            //head.Append("://");
            //head.Append(incoming.DomainName);
            //head.Append(':');
            //head.Append(incoming.DestinationPort);
            head.Append(incoming.Path.HasValue ? incoming.Path.Value : "/");
            if (incoming.Query.HasValue)
                head.Append(incoming.Query);
            head.Append(' ');
            head.Append("HTTP/1.1");
            head.Append("\r\n");
            head.Append("Host: ");
            head.Append(incoming.DomainName);
            head.Append(":");
            head.Append(incoming.DestinationPort);
            head.Append("\r\n");

            if (incoming.Headers != null)
            {
                foreach (var header in incoming.Headers)
                {
                    if (string.IsNullOrEmpty(header.Key))
                        continue;

                    if (string.Equals("Connection", header.Key, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals("Keep-Alive", header.Key, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals("Host", header.Key, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (header.Value == null || header.Value.Length == 0)
                        continue;

                    foreach (var headValue in header.Value)
                    {
                        head.Append(header.Key);
                        head.Append(": ");
                        head.Append(headValue);
                        head.Append("\r\n");
                    }
                }
            }

            head.Append("Connection: Keep-Alive\r\n");
            head.Append("Keep-Alive: timeout=");
            head.Append((int) (_connection.MaximumIdleTime.TotalSeconds + 10));
            head.Append("\r\n\r\n");

            var headBytes = Encoding.ASCII.GetBytes(head.ToString());

            _context.Log?.Log(LogType.TcpIp, LogLevel.Detailed,
                () => $"Writing {headBytes.Length} bytes of header to the connection stream");

            if (_context.Log != null && _context.Log.WillLog(LogType.TcpIp, LogLevel.VeryDetailed))
            {
                var headLines = head.ToString().Replace("\r", "").Split('\n');
                foreach (var headLine in headLines.Where(h => !string.IsNullOrEmpty(h)))
                    _context.Log.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"> {headLine}");
            }

            _incomingWrite.Started(_connection.Stream.BeginWrite(headBytes, 0, headBytes.Length, null, null), IncomingWriteContentStep);

            if (_context.Incoming.Content == null || !_context.Incoming.Content.CanRead)
                _incomingRead.NextStep = null;
            else
                _incomingRead.NextStep = IncomingReadContentStep;

            _outgoingRead.NextStep = OutgoingReadHeaderStep;
        }

        private void IncomingReadContentStep()
        {
            if (_incomingRead.IsIdle)
            {
                var buffer = _bufferPool.Get();
                _incomingRead.Buffer = new Buffer { Data = buffer };
                _incomingRead.Started(_context.Incoming.Content.BeginRead(buffer, 0, buffer.Length, null, null));
                return;
            }

            if (!_incomingRead.IsComplete)
                return;

            _incomingRead.Buffer.Length = _context.Incoming.Content.EndRead(_incomingRead.Result);

            _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_incomingRead.Name} received {_incomingRead.Buffer.Length} bytes of content");

            if (_incomingRead.Buffer.Length > 0)
            {
                _incomingBuffers.Prepend(_incomingRead.Buffer);

                var buffer = _bufferPool.Get();
                _incomingRead.Buffer = new Buffer { Data = buffer };
                _incomingRead.Started(_context.Incoming.Content.BeginRead(buffer, 0, buffer.Length, null, null));
            }
            else
            {
                _bufferPool.Reuse(_incomingRead.Buffer.Data);
                _incomingRead.NextStep = null;
            }
        }

        private void IncomingWriteContentStep()
        {
            if (_incomingWrite.IsComplete)
            {
                if (!_incomingWrite.IsIdle)
                    _connection.Stream.EndWrite(_outgoingWrite.Result);

                if (_incomingWrite.Buffer != null)
                    _bufferPool.Reuse(_incomingWrite.Buffer.Data);

                _incomingWrite.Buffer = _incomingBuffers.PopLast();

                if (_incomingWrite.Buffer == null)
                {
                    if (_incomingRead.NextStep == null)
                        _incomingWrite.Clear();
                    else
                        _incomingWrite.Result = null;
                }
                else
                {
                    _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_incomingWrite.Name} writing {_incomingWrite.Buffer.Length} bytes");
                    _incomingWrite.Started(_connection.Stream.BeginWrite(_incomingWrite.Buffer.Data, 0, _incomingWrite.Buffer.Length, null, null));
                }
            }
        }

        #endregion

        #region Outgoing steps

        private void OutgoingReadHeaderStep()
        {
            if (_outgoingRead.IsIdle)
            {
                var buffer = _bufferPool.Get();
                _outgoingRead.Buffer = new Buffer { Data = buffer };
                _outgoingRead.Started(_connection.Stream.BeginRead(buffer, 0, buffer.Length, null, null));
                _context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => _outgoingRead.Name + " waiting for headers");
                return;
            }

            if (!_outgoingRead.IsComplete)
                return;

            _outgoingRead.Buffer.Length = _connection.Stream.EndRead(_outgoingRead.Result);

            _context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"{_outgoingRead.Name} received {_outgoingRead.Buffer.Length} bytes");

            int contentStartOffset = _outgoingRead.AppendHeader(_outgoingRead.Buffer);

            if (contentStartOffset > 0)
            {
                var contentLength = _outgoingRead.Buffer.Length - contentStartOffset;
                _outgoingRead.ContentBytesReceived += contentLength;
                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_outgoingRead.Name} {contentLength} of the bytes received are content");

                var buffer = new Buffer
                {
                    Data = _bufferPool.Get(contentLength),
                    Length = contentLength
                };
                Array.Copy(_outgoingRead.Buffer.Data, contentStartOffset, buffer.Data, 0, contentLength);
                _outgoingBuffers.Prepend(buffer);
            }

            if (_outgoingRead.HeaderComplete)
            {
                var outgoing = _context.Outgoing;

                _outgoingRead.ParseHeaders(
                    status => outgoing.StatusCode = status,
                    reason => outgoing.ReasonPhrase = reason,
                    (name, value) =>
                    {
                        if (_context.Outgoing.Headers.ContainsKey(name))
                        {
                            var originalHeaders = _context.Outgoing.Headers[name];
                            var newHeaders = new string[originalHeaders.Length + 1];
                            Array.Copy(originalHeaders, 0, newHeaders, 0, originalHeaders.Length);
                            newHeaders[originalHeaders.Length] = value;
                            _context.Outgoing.Headers[name] = newHeaders;
                        }
                        else
                        {
                            _context.Outgoing.Headers[name] = new[] { value };
                        }
                    });

                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed,
                    () => $"Sending {_outgoingRead.HeaderLines.Count} headers in outgoing message");

                _context.Outgoing.SendHeaders(_context);

                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed,
                    () => $"Setting Tcp client receive timeout to " + _readTimeoutMs);
                _connection.ReceiveTimeoutMs = _readTimeoutMs;
                _outgoingRead.Timeout = TimeSpan.FromMilliseconds(_readTimeoutMs);

                _outgoingRead.NextStep = OutgoingReadContentStep;
                _outgoingWrite.NextStep = OutgoingWriteContentStep;
            }
        }

        private void OutgoingReadContentStep()
        {
            if (!_outgoingRead.IsIdle)
            {
                try
                {
                    if (!_outgoingRead.IsComplete)
                        return;
                }
                catch (RequestStreamTimeoutException ex)
                {
                    if (_outgoingRead.ContentLength.HasValue && _outgoingRead.ContentBytesReceived < _outgoingRead.ContentLength.Value)
                        throw new RequestStreamException($"{_outgoingRead.Name} was expecting {_outgoingRead.ContentLength.Value} bytes of content but only received {_outgoingRead.ContentBytesReceived}");
                }

                _outgoingRead.Buffer.Length = _connection.Stream.EndRead(_outgoingRead.Result);
                _outgoingRead.ContentBytesReceived += _outgoingRead.Buffer.Length;
                _outgoingBuffers.Prepend(_outgoingRead.Buffer);

                _context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"{_outgoingRead.Name} received {_outgoingRead.Buffer.Length} bytes");
            }

            var buffer = _bufferPool.Get();
            _outgoingRead.Buffer = new Buffer { Data = buffer };
            _outgoingRead.Started(_connection.Stream.BeginRead(buffer, 0, buffer.Length, null, null));
            _context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => _outgoingRead.Name + " waiting for content");
        }

        private void OutgoingWriteContentStep()
        {
            if (_outgoingWrite.IsComplete)
            {
                if (!_outgoingWrite.IsIdle)
                    _context.Outgoing.Content.EndWrite(_outgoingWrite.Result);

                if (_outgoingWrite.Buffer != null)
                    _bufferPool.Reuse(_outgoingWrite.Buffer.Data);

                _outgoingWrite.Buffer = _outgoingBuffers.PopLast();

                if (_outgoingWrite.Buffer == null)
                {
                    if (_outgoingRead.NextStep == null)
                        _outgoingWrite.Clear();
                    else
                        _outgoingWrite.Result = null;
                }
                else
                {
                    _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_outgoingWrite.Name} writing {_outgoingWrite.Buffer.Length} bytes");
                    _outgoingWrite.Started(_context.Outgoing.Content.BeginWrite(_outgoingWrite.Buffer.Data, 0, _outgoingWrite.Buffer.Length, null, null));
                }
            }
        }

        #endregion

        private class Buffer
        {
            public byte[] Data;
            public int Length;
        }

        private class Endpoint
        {
            public Endpoint(ILog log, string name, TimeSpan timeout)
            {
                _log = log;
                Name = name;
                Start = DateTime.UtcNow;
                Timeout = timeout;
            }

            protected ILog _log;

            public string Name;
            public Action NextStep;
            public IAsyncResult Result;
            public DateTime Start;
            public TimeSpan Timeout;
            public Buffer Buffer;

            public void Clear()
            {
                NextStep = null;
                Result = null;
                Start = DateTime.UtcNow;
                Buffer = null;
            }

            public void Started(IAsyncResult result, Action nextStep = null)
            {
                Result = result;
                Start = DateTime.UtcNow;
            }

            public bool IsIdle => Result == null;

            public bool IsComplete
            {
                get
                {
                    if (Result == null) return true;

                    if (Start + Timeout < DateTime.UtcNow)
                    {
                        var msg = $"{Name} did not complete within {Timeout}";
                        _log?.Log(LogType.TcpIp, LogLevel.Important, () => msg);
                        throw new RequestStreamTimeoutException(msg);
                    }

                    return Result.IsCompleted;
                }
            }
        }

        private class HttpReceiveEndpoint: Endpoint
        {
            public bool CanHaveContent;
            public int? ContentLength;
            public int ContentBytesReceived;
            public StringBuilder Line;
            public System.Collections.Generic.List<string> HeaderLines;
            public bool HeaderComplete;

            private bool _beginning;

            public HttpReceiveEndpoint(ILog log, string name, TimeSpan timeout, bool canHaveContent)
                : base(log, name, timeout)
            {
                CanHaveContent = canHaveContent;
                ContentLength = canHaveContent ? null : (int?) 0;
                HeaderLines = new System.Collections.Generic.List<string>();
                _beginning = true;
            }

            public int AppendHeader(Buffer buffer)
            {
                for (var i = 0; i < buffer.Length; i++)
                {
                    var c = (char)buffer.Data[i];

                    if (_beginning && c != 'H') continue;
                    if (c == '\r') continue;

                    if (c == '\n')
                    {
                        if (_beginning) continue;

                        if (Line.Length == 0)
                        {
                            HeaderComplete = true;
                            return i + 1;
                        }

                        HeaderLines.Add(Line.ToString());
                        Line.Clear();
                        continue;
                    }

                    Line.Append(c);
                    _beginning = false;
                }

                return 0;
            }

            public void ParseHeaders(
                Action<ushort> storeStatusCode, 
                Action<string> storeReasonPhrase,
                Action<string, string> storeHeaderValue)
            {
                _log?.Log(LogType.TcpIp, LogLevel.VeryDetailed,
                    () => $"First blank line received, parsing headers");

                for (var j = 0; j < HeaderLines.Count; j++)
                    _log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"> {HeaderLines[j]}");

                var firstSpaceIndex = HeaderLines[0].IndexOf(' ');

                _log?.Log(LogType.TcpIp, LogLevel.VeryDetailed,
                    () => $"First space in first header line at {firstSpaceIndex}");

                if (firstSpaceIndex < 1)
                    throw new RequestStreamException("response first line contains no spaces");

                var secondSpaceIndex = HeaderLines[0].IndexOf(' ', firstSpaceIndex + 1);

                _log?.Log(LogType.TcpIp, LogLevel.VeryDetailed,
                    () => $"Second space in first header line at {secondSpaceIndex}");

                if (secondSpaceIndex < 3)
                {
                    _log?.Log(LogType.TcpIp, LogLevel.Standard,
                        () => "Response first line contains only 1 space");

                    throw new RequestStreamException(Name + " response first line contains only 1 space");
                }

                var statusString = HeaderLines[0].Substring(firstSpaceIndex + 1, secondSpaceIndex - firstSpaceIndex);
                if (ushort.TryParse(statusString, out var statusCode))
                {
                    _log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"Response status code {statusCode}");
                    storeStatusCode(statusCode);
                }
                else
                {
                    _log?.Log(LogType.TcpIp, LogLevel.Standard,
                        () => $"Response status code '{statusString}' can not be parsed as ushort");

                    throw new RequestStreamException(Name + " response status code can not be parsed as a number");
                }

                var reasonPhrase = HeaderLines[0].Substring(secondSpaceIndex + 1);
                _log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"Response reason phrase '{reasonPhrase}'");
                storeReasonPhrase(reasonPhrase);

                for (var j = 1; j < HeaderLines.Count; j++)
                {
                    var headerLine = HeaderLines[j];

                    var colonPos = headerLine.IndexOf(':');
                    if (colonPos > 0)
                    {
                        var name = headerLine.Substring(0, colonPos).Trim();
                        var value = headerLine.Substring(colonPos + 1).Trim();
                        storeHeaderValue(name, value);

                        if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                        {
                            _log?.Log(LogType.TcpIp, LogLevel.VeryDetailed,
                                () => $"Content length header = {value}");

                            if (int.TryParse(value, out var i))
                            {
                                ContentLength = i;
                            }
                            else
                            {
                                _log?.Log(LogType.TcpIp, LogLevel.Standard,
                                    () => $"Content length header '{value}' is not an integer");
                            }
                        }
                    }
                    else
                    {
                        _log?.Log(LogType.TcpIp, LogLevel.Standard,
                            () => $"Invalid header line '{headerLine}' does not contain a colon");

                        throw new RequestStreamException(Name + " header line does not contain a colon");
                    }
                }
            }
        }
    }
}