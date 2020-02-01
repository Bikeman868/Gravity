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
    /// Provides a wrapper around the outgoing and incoming data stream that
    /// allows a small pool of threads to manage a large number of concurrent
    /// TCP connections. Note that the outgoing and incoming direction each read
    /// from an input stream and write to an output stream so there are 4 streams
    /// altogether.
    /// </summary>
    internal class RequestStream: IDisposable
    {
        private readonly IBufferPool _bufferPool;
        private readonly TaskCompletionSource<bool> _taskCompletionSource;
        private readonly LinkedList<Buffer> _outgoingBuffers;
        private readonly LinkedList<Buffer> _incomingBuffers;
        private readonly AutoResetEvent _event;

        private Connection _connection;
        private bool _reuseConnection;
        private IRequestContext _context;
        private TimeSpan _responseTimeout;
        private int _readTimeoutMs;
        private bool _isCompleted;

        /// <summary>
        /// Maintains state for reading responses from the back-end server connection
        /// </summary>
        private ConnectionReceiveEndpoint _outgoingRead;

        /// <summary>
        /// Maintains state for writng responses back to the outside world
        /// </summary>
        private ContextSendEndpoint _outgoingWrite;

        /// <summary>
        /// Maintains state for reading requests from the outside world
        /// </summary>
        private ContextReceiveEndpoint _incomingRead;

        /// <summary>
        /// Maintains state for writing requests to the back-end server connection
        /// </summary>
        private ConnectionSendEndpoint _incomingWrite;

        public Task<bool> Task => _taskCompletionSource.Task;

        public RequestStream(            
            IBufferPool bufferPool)
        {
            _bufferPool = bufferPool;
            _event = new AutoResetEvent(true);
            _outgoingBuffers = new LinkedList<Buffer>();
            _incomingBuffers = new LinkedList<Buffer>();
            _taskCompletionSource = new TaskCompletionSource<bool>();
        }

        public void Dispose()
        {
            _event.Dispose();
        }

        public RequestStream Start(
            Connection connection,
            IRequestContext context, 
            TimeSpan responseTimeout, 
            int readTimeoutMs,
            bool reuseConnection)
        {
            _connection = connection;
            _context = context;
            _responseTimeout = responseTimeout;
            _readTimeoutMs = readTimeoutMs;
            _reuseConnection = reuseConnection;

            var incomingCanHaveContent =
                !string.Equals(context.Incoming.Method, "HEAD", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(context.Incoming.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase);

            _incomingRead = new ContextReceiveEndpoint(context.Log, "Incoming read", TimeSpan.FromSeconds(5));
            _incomingWrite = new ConnectionSendEndpoint(context.Log, "Incoming write", TimeSpan.FromSeconds(5));
            _outgoingRead = new ConnectionReceiveEndpoint(context.Log, "Outgoing read", responseTimeout, incomingCanHaveContent);
            _outgoingWrite = new ContextSendEndpoint(context.Log, "Outgoing write", TimeSpan.FromSeconds(5));

            _incomingWrite.NextStep = IncomingWriteHeaderStep;

            _connection.BeginTransaction(context.Log);

            return this;
        }

        /// <summary>
        /// Checks for anything to do on an this stream returning immediately
        /// so that the thread can check other active connections.
        /// </summary>
        /// <returns>Returns true if there are more steps to complete</returns>
        public bool NextStep()
        {
            var keepRunning = true;
            var success = true;

            // Return immediately if another thread is already servicing this request
            if (!_event.WaitOne(0))
                return keepRunning;

            if (_isCompleted)
                return keepRunning;

            try
            {
                if (_outgoingRead.NextStep == null && 
                    _outgoingWrite.NextStep == null &&
                    _incomingRead.NextStep == null &&
                    _incomingWrite.NextStep == null)
                    return false;

                if (_outgoingRead.NextStep != null) _outgoingRead.NextStep();
                if (_incomingRead.NextStep != null) _incomingRead.NextStep();
                if (_outgoingWrite.NextStep != null) _outgoingWrite.NextStep();
                if (_incomingWrite.NextStep != null) _incomingWrite.NextStep();

                if (_outgoingRead.NextStep == null && 
                    _outgoingWrite.NextStep == null &&
                    _incomingRead.NextStep == null &&
                    _incomingWrite.NextStep == null)
                {
                    keepRunning = false;
                    _isCompleted = true;
                _connection.EndTransaction(_context.Log, _reuseConnection);
                }
            }
            catch
            {
                success = false;
                _isCompleted = true;

                _outgoingRead.Stop();
                _outgoingWrite.Stop();
                _incomingRead.Stop();
                _incomingWrite.Stop();

                _connection.EndTransaction(_context.Log, false);
            }
            finally
            {
                if (_isCompleted)
                {
                    // Release tasks waiting for this request stream
                    _taskCompletionSource.SetResult(success);
                }

                // Allow other thread pool threads to service this connection
                _event.Set();
            }
            return keepRunning;
        }

        #region Incoming steps

        private void IncomingWriteHeaderStep()
        {
            var incoming = _context.Incoming;

            _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_incomingWrite.Name} finalizing incoming headers");
            incoming.SendHeaders(_context);

            var head = new StringBuilder();

            head.Append(incoming.Method);
            head.Append(' ');
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

            _context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"{_incomingWrite.Name} writing {headBytes.Length} bytes of header to the connection stream");
            if (_context.Log != null && _context.Log.WillLog(LogType.TcpIp, LogLevel.VeryDetailed))
            {
                var headLines = head.ToString().Replace("\r", "").Split('\n');
                foreach (var headLine in headLines.Where(h => !string.IsNullOrEmpty(h)))
                    _context.Log.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"  > {headLine}");
            }

            _incomingWrite.AsyncStart(_connection.Stream.BeginWrite(headBytes, 0, headBytes.Length, null, null), IncomingWriteContentStep);

            if (_context.Incoming.Content == null || !_context.Incoming.Content.CanRead)
            {
                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_incomingRead.Name} there is no incoming content");
                _incomingRead.Stop();
            }
            else
            {
                _context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"{_incomingWrite.Name} next step is writing content");
                _incomingWrite.NextStep = IncomingWriteContentStep;

                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_incomingRead.Name} next step is reading content");
                _incomingRead.NextStep = IncomingReadContentStep;
            }

            _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_outgoingRead.Name} setting receive timeout to {_responseTimeout}");
            _connection.ReceiveTimeoutMs = (int)_responseTimeout.TotalMilliseconds;

            _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_outgoingRead.Name} next step is reading header");
            _outgoingRead.NextStep = OutgoingReadHeaderStep;
        }

        private void IncomingReadContentStep()
        {
            if (_incomingRead.Result == null)
            {
                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_incomingRead.Name} waiting for incoming content");

                var buffer = _bufferPool.Get();
                _incomingRead.Buffer = new Buffer { Data = buffer };
                _incomingRead.AsyncStart(_context.Incoming.Content.BeginRead(buffer, 0, buffer.Length, null, null));
                return;
            }

            var isComplete = _incomingRead.IsComplete;
            
            if (!isComplete.HasValue)
                throw new RequestStreamTimeoutException($"{_incomingRead.Name} timeout waiting for content");

            if (!isComplete.Value)
                return;

            var bytesRead = _context.Incoming.Content.EndRead(_incomingRead.Result);
            _incomingRead.AsyncEnd();
            _incomingRead.Buffer.Length = bytesRead;

            _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_incomingRead.Name} received {bytesRead} bytes of content");

            if (bytesRead > 0)
            {
                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_incomingRead.Name} queuing {bytesRead} bytes of content");
                _incomingBuffers.Prepend(_incomingRead.Buffer);

                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_incomingRead.Name} waiting for more incoming content");
                var buffer = _bufferPool.Get();
                _incomingRead.Buffer = new Buffer { Data = buffer };
                _incomingRead.AsyncStart(_context.Incoming.Content.BeginRead(buffer, 0, buffer.Length, null, null));
            }
            else
            {
                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_incomingRead.Name} finished reading content");
                _bufferPool.Reuse(_incomingRead.Buffer.Data);
                _incomingRead.NextStep = null;
            }
        }

        private void IncomingWriteContentStep()
        {
            var isComplete = _incomingWrite.IsComplete;
            
            if (!isComplete.HasValue)
                throw new RequestStreamTimeoutException($"{_incomingWrite.Name} timeout sending content");

            if (!isComplete.Value)
                return;

            if (_incomingWrite.Result != null)
            {
                _connection.Stream.EndWrite(_incomingWrite.Result);
                _incomingWrite.AsyncEnd();
            }

            if (_incomingWrite.Buffer != null)
                _bufferPool.Reuse(_incomingWrite.Buffer.Data);

            _incomingWrite.Buffer = _incomingBuffers.PopLast();

            if (_incomingWrite.Buffer == null)
            {
                if (_incomingRead.NextStep == null)
                    _incomingWrite.Stop();
            }
            else
            {
                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_incomingWrite.Name} writing {_incomingWrite.Buffer.Length} bytes");
                _incomingWrite.AsyncStart(_connection.Stream.BeginWrite(_incomingWrite.Buffer.Data, 0, _incomingWrite.Buffer.Length, null, null));
            }
        }

        #endregion

        #region Outgoing steps

        private void OutgoingReadHeaderStep()
        {
            if (_outgoingRead.Result == null)
            {
                if (_connection.HasPendingRead(out var result, out var buffer))
                {
                    _outgoingRead.Buffer = new Buffer { Data = buffer };
                    _outgoingRead.AsyncStart(result);
                }
                else
                {
                    buffer = _bufferPool.Get();
                    _outgoingRead.Buffer = new Buffer { Data = buffer };
                    _outgoingRead.AsyncStart(_connection.BeginRead(buffer));
                }
                _context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"{_outgoingRead.Name} waiting for headers");
                return;
            }

            var isComplete = _outgoingRead.IsComplete;
            
            if (!isComplete.HasValue)
                throw new RequestStreamTimeoutException($"{_outgoingRead.Name} timeout reading header");

            if (!isComplete.Value)
                return;

            var bytesRead = _connection.EndRead();
            _outgoingRead.AsyncEnd();
            _outgoingRead.Buffer.Length = bytesRead;

            _context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"{_outgoingRead.Name} received {bytesRead} bytes of header");

            int contentStartOffset = _outgoingRead.AppendHeader(_outgoingRead.Buffer);

            if (contentStartOffset > 0)
            {
                var contentLength = _outgoingRead.Buffer.Length - contentStartOffset;
                if (contentLength > 0)
                {
                    _outgoingRead.ContentBytesReceived += contentLength;
                    _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_outgoingRead.Name} {contentLength} of the bytes received are content");

                    var buffer = new Buffer
                    {
                        Data = _bufferPool.Get(contentLength),
                        Length = contentLength
                    };
                    Array.Copy(_outgoingRead.Buffer.Data, contentStartOffset, buffer.Data, 0, contentLength);

                    _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_outgoingRead.Name} queuing {contentLength} bytes of received content");
                    _outgoingBuffers.Prepend(buffer);
                }
            }

            _bufferPool.Reuse(_outgoingRead.Buffer.Data);
            _outgoingRead.Buffer = null;

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
                            outgoing.Headers[name] = newHeaders;
                        }
                        else
                        {
                            outgoing.Headers[name] = new[] { value };
                        }
                    });

                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_outgoingRead.Name} set {_outgoingRead.HeaderLines.Count} headers in outgoing message");

                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_outgoingWrite.Name} finalizing headers in outgoing message");
                _context.Outgoing.SendHeaders(_context);

                if (!_outgoingRead.CanHaveContent)
                {
                    _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_outgoingRead.Name} finished because no content is expected");
                    _outgoingRead.Stop();
                    _outgoingWrite.Stop();
                    return;
                }

                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_outgoingRead.Name} setting Tcp client receive timeout to {_readTimeoutMs}ms");
                _connection.ReceiveTimeoutMs = _readTimeoutMs;
                _outgoingRead.Timeout = TimeSpan.FromMilliseconds(_readTimeoutMs);

                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_outgoingRead.Name} next outgoing step is reading and writing content");
                _outgoingRead.NextStep = OutgoingReadContentStep;
                _outgoingWrite.NextStep = OutgoingWriteContentStep;
            }
        }

        private void OutgoingReadContentStep()
        {
            if (_outgoingRead.Result != null)
            {
                var readCompleted = _outgoingRead.IsComplete;

                if (!readCompleted.HasValue)
                {
                    if (_outgoingRead.ContentLength.HasValue && _outgoingRead.ContentBytesReceived < _outgoingRead.ContentLength.Value)
                        throw new RequestStreamException($"{_outgoingRead.Name} was expecting {_outgoingRead.ContentLength.Value} bytes of content but only received {_outgoingRead.ContentBytesReceived}");
                    
                    _context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"{_outgoingRead.Name} no more bytes received after {_outgoingRead.Timeout} assuming end of stream");
                    _outgoingRead.AsyncEnd();
                    _bufferPool.Reuse(_outgoingRead.Buffer.Data);
                    _outgoingRead.Stop();
                    return;
                }

                if (readCompleted == false)
                    return;

                var bytesRead = _connection.EndRead();
                _outgoingRead.AsyncEnd();

                if (bytesRead == 0)
                {
                    _context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"{_outgoingRead.Name} no more bytes in the stream");
                    _bufferPool.Reuse(_outgoingRead.Buffer.Data);
                    _outgoingRead.Stop();
                    return;
                }

                _context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"{_outgoingRead.Name} received {bytesRead} bytes and is queuing them for output");

                _outgoingRead.Buffer.Length = bytesRead;
                _outgoingRead.ContentBytesReceived += bytesRead;
                _outgoingBuffers.Prepend(_outgoingRead.Buffer);
            }

            if (_connection.HasPendingRead(out var result, out var buffer))
            {
                _outgoingRead.Buffer = new Buffer { Data = buffer };
                _outgoingRead.AsyncStart(result);
            }
            else
            {
                buffer = _bufferPool.Get();
                _outgoingRead.Buffer = new Buffer { Data = buffer };
                _outgoingRead.AsyncStart(_connection.BeginRead(buffer));
            }
            _context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"{_outgoingRead.Name} waiting for content");
        }

        private void OutgoingWriteContentStep()
        {
            var isComplete = _outgoingWrite.IsComplete;
            
            if (!isComplete.HasValue)
                throw new RequestStreamTimeoutException($"{_outgoingWrite.Name} timeout writing content");

            if (!isComplete.Value)
                return;

            if (_outgoingWrite.Result != null)
            {
                _context.Outgoing.Content.EndWrite(_outgoingWrite.Result);
                _outgoingWrite.AsyncEnd();
            }

            if (_outgoingWrite.Buffer != null)
                _bufferPool.Reuse(_outgoingWrite.Buffer.Data);

            _outgoingWrite.Buffer = _outgoingBuffers.PopLast();

            if (_outgoingWrite.Buffer == null)
            {
                if (_outgoingRead.NextStep == null)
                {
                    _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_outgoingWrite.Name} has no more data to write");
                    _outgoingWrite.Stop();
                }
            }
            else
            {
                _context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{_outgoingWrite.Name} writing {_outgoingWrite.Buffer.Length} bytes");
                _outgoingWrite.AsyncStart(_context.Outgoing.Content.BeginWrite(_outgoingWrite.Buffer.Data, 0, _outgoingWrite.Buffer.Length, null, null));
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

            public void Stop()
            {
                NextStep = null;
            }

            public void AsyncStart(IAsyncResult result, Action nextStep = null)
            {
                Result = result;
                Start = DateTime.UtcNow;
                if (nextStep != null) NextStep = nextStep;
            }

            public void AsyncEnd(Action nextStep = null)
            {
                Result = null;
                if (nextStep != null) NextStep = nextStep;
            }

            public bool? IsComplete
            {
                get
                {
                    if (Result == null) return true;

                    if (Start + Timeout < DateTime.UtcNow)
                    {
                        var msg = $"{Name} waited for {DateTime.UtcNow - Start} and the timeout is {Timeout}";
                        _log?.Log(LogType.TcpIp, LogLevel.Detailed, () => msg);
                        return null;
                    }

                    return Result.IsCompleted;
                }
            }
        }

        private class ContextSendEndpoint : Endpoint
        {
            public ContextSendEndpoint(ILog log, string name, TimeSpan timeout)
                : base(log, name, timeout)
            {
            }
        }

        private class ContextReceiveEndpoint : Endpoint
        {
            public ContextReceiveEndpoint(ILog log, string name, TimeSpan timeout)
                : base(log, name, timeout)
            {
            }
        }

        private class ConnectionSendEndpoint : Endpoint
        {
            public ConnectionSendEndpoint(ILog log, string name, TimeSpan timeout)
                : base(log, name, timeout)
            {
            }
        }

        private class ConnectionReceiveEndpoint: Endpoint
        {
            public bool CanHaveContent;
            public int? ContentLength;
            public int ContentBytesReceived;
            public StringBuilder Line;
            public System.Collections.Generic.List<string> HeaderLines;
            public bool HeaderComplete;

            private bool _beginning;

            public ConnectionReceiveEndpoint(ILog log, string name, TimeSpan timeout, bool canHaveContent)
                : base(log, name, timeout)
            {
                CanHaveContent = canHaveContent;
                ContentLength = canHaveContent ? null : (int?) 0;
                HeaderLines = new System.Collections.Generic.List<string>();
                Line = new StringBuilder();
                _beginning = true;

                log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => canHaveContent ? $"{name} might have content" : $"{name} not expecting any content");
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
                _log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{Name} first blank line received, parsing headers");

                for (var j = 0; j < HeaderLines.Count; j++)
                    _log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"  > {HeaderLines[j]}");

                var firstSpaceIndex = HeaderLines[0].IndexOf(' ');

                _log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{Name} first space in first header line at {firstSpaceIndex}");

                if (firstSpaceIndex < 1)
                    throw new RequestStreamException(Name + " response first line contains no spaces");

                var secondSpaceIndex = HeaderLines[0].IndexOf(' ', firstSpaceIndex + 1);

                _log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{Name} second space in first header line at {secondSpaceIndex}");

                if (secondSpaceIndex < 3)
                {
                    _log?.Log(LogType.TcpIp, LogLevel.Standard, () => $"{Name} Response first line contains only 1 space");

                    throw new RequestStreamException(Name + " response first line contains only 1 space");
                }

                var statusString = HeaderLines[0].Substring(firstSpaceIndex + 1, secondSpaceIndex - firstSpaceIndex);
                if (ushort.TryParse(statusString, out var statusCode))
                {
                    _log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"{Name} response status code {statusCode}");
                    storeStatusCode(statusCode);
                }
                else
                {
                    _log?.Log(LogType.TcpIp, LogLevel.Standard,
                        () => $"{Name} response status code '{statusString}' can not be parsed as ushort");

                    throw new RequestStreamException(Name + " response status code can not be parsed as a number");
                }

                var reasonPhrase = HeaderLines[0].Substring(secondSpaceIndex + 1);
                _log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"{Name} response reason phrase '{reasonPhrase}'");
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
                            _log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{Name} content length header = {value}");

                            if (int.TryParse(value, out var i))
                            {
                                ContentLength = i;
                            }
                            else
                            {
                                _log?.Log(LogType.TcpIp, LogLevel.Standard,
                                    () => $"{Name} content length header '{value}' is not an integer");
                            }
                        }
                    }
                    else
                    {
                        _log?.Log(LogType.TcpIp, LogLevel.Standard, () => $"{Name} invalid header line '{headerLine}' does not contain a colon");

                        throw new RequestStreamException(Name + " header line does not contain a colon");
                    }
                }
            }
        }
    }
}