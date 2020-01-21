using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;

namespace Gravity.Server.ProcessingNodes.Server
{
    internal class ConnectionException: ApplicationException
    {
        public Connection Connection;

        public ConnectionException(Connection connection, string message, Exception innerException = null):
            base("Connection " + message, innerException)
        {
            Connection = connection;
        }
    }

    internal enum ConnectionState
    {
        New,
        Connected,
        Busy,
        Old,
        Disconnected,
        Pending,
        Fault
    }

    internal class Connection: IDisposable
    {
        private readonly TimeSpan _maximumIdleTime = TimeSpan.FromMinutes(5);
        private readonly IBufferPool _bufferPool;
        private readonly IPEndPoint _endpoint;
        private readonly string _domainName;
        private readonly TimeSpan _connectionTimeout;
        private readonly Scheme _scheme;

        private TcpClient _tcpClient;
        private Stream _stream;
        private DateTime _lastUsedUtc;
        private Task _pendingTask;

        public Connection(
            IBufferPool bufferPool,
            IPEndPoint endpoint,
            string domainName,
            Scheme scheme,
            TimeSpan connectionTimeout)
        {
            _bufferPool = bufferPool;
            _endpoint = endpoint;
            _domainName = domainName;
            _scheme = scheme;
            _connectionTimeout = connectionTimeout;
            State = ConnectionState.New;
        }

        public void Dispose()
        {
            _stream?.Close();
            _tcpClient?.Close();
            State = ConnectionState.Disconnected;
        }

        public Task Connect(ILog log)
        {
            log?.Log(LogType.TcpIp, LogLevel.Standard, 
                () => $"Opening a new Tcp connection to {_scheme.ToString().ToLower()}://{_domainName}:{_endpoint.Port} at {_endpoint.Address}");

            _tcpClient = new TcpClient
            {
                ReceiveTimeout = 0,
                SendTimeout = 0
            };

            return _tcpClient.ConnectAsync(_endpoint.Address, _endpoint.Port)
                .ContinueWith(connectTask => 
                { 
                    if (connectTask.IsFaulted)
                    {
                        log?.Log(LogType.Exception, LogLevel.Important, () => $"Failed to connect. {connectTask.Exception?.Message}");
                        throw new ConnectionException(this, "Exception in TcpClient", connectTask.Exception);
                    }

                    if (connectTask.IsCanceled)
                    {
                        log?.Log(LogType.Exception, LogLevel.Important, () => $"Failed to connect within {_connectionTimeout}");
                        throw new ConnectionException(this, "TcpClient connection was cancelled");
                    }

                    State = ConnectionState.Connected;

                    _stream = _tcpClient.GetStream();

                    if (_scheme == Scheme.Https)
                    {
                        log?.Log(LogType.TcpIp, LogLevel.Standard, () => "Wrapping Tcp connection in SSL stream");
                        var sslStream = new SslStream(_stream);

                        _stream = sslStream;

                        log?.Log(LogType.TcpIp, LogLevel.Standard, () => $"Authenticating server's SSL certificate for {_domainName}");
                        sslStream.AuthenticateAsClient(_domainName);
                    }

                    _lastUsedUtc = DateTime.UtcNow;
                });
        }

        public ConnectionState State { get; private set; }

        public bool IsAvailable
        {
            get
            {
                switch (State)
                {
                    case ConnectionState.New:
                    case ConnectionState.Old:
                        return true;
                    case ConnectionState.Connected:
                        if (DateTime.UtcNow - _lastUsedUtc > _maximumIdleTime)
                            State = ConnectionState.Old;
                        return true;
                    case ConnectionState.Pending:
                        if (_pendingTask != null && _pendingTask.IsCompleted)
                        {
                            State = ConnectionState.Connected;
                            _pendingTask = null;
                            return true;
                        }
                        return false;
                    default:
                        return false;
                }
            }
        }

        public Task Send(IRequestContext context, TimeSpan responseTimeout, int readTimeoutMs)
        {
            State = ConnectionState.Busy;
            _tcpClient.ReceiveTimeout = (int)responseTimeout.TotalMilliseconds;

            return Task.WhenAll(SendHttp(context), ReceiveHttp(context, readTimeoutMs))
                .ContinueWith(t => _lastUsedUtc = DateTime.UtcNow);
        }

        private Task SendHttp(IRequestContext context)
        {
            var incoming = context.Incoming;
            incoming.SendHeaders(context);

            var head = new StringBuilder();

            head.Append(incoming.Method);
            head.Append(' ');
            //head.Append(incoming.Scheme == Scheme.Https ? "https" : " http");
            //head.Append("://");
            //head.Append(incoming.DomainName);
            //head.Append(':');
            //head.Append(incoming.DestinationPort);
            head.Append(incoming.Path);
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
            head.Append((int)(_maximumIdleTime.TotalSeconds + 5));
            head.Append("\r\n\r\n");

            var headBytes = Encoding.ASCII.GetBytes(head.ToString());

            context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"Writing {headBytes.Length} bytes of header to the connection stream");
            _stream.Write(headBytes, 0, headBytes.Length);

            return Task.Run(() =>
            {
                if (incoming.Content != null && incoming.Content.CanRead)
                {
                    var buffer = incoming.ContentLength.HasValue ? _bufferPool.Get(incoming.ContentLength.Value) : _bufferPool.Get();
                    try
                    {
                        int Read()
                        {
                            var readTask = incoming.Content.ReadAsync(buffer, 0, buffer.Length);
                            readTask.Wait();

                            if (readTask.IsFaulted)
                            {
                                context.Log?.Log(LogType.Exception, LogLevel.Important, () => $"Connection failed to read from the incomming stream. {readTask.Exception.Message}");
                                throw new ConnectionException(this, "incomming read task faulted", readTask.Exception);
                            }

                            if (readTask.IsCanceled)
                            {
                                context.Log?.Log(LogType.TcpIp, LogLevel.Important, () => $"Timeout reading from the incomming stream");
                                throw new ConnectionException(this, "read task timed out");
                            }

                            return readTask.Result;
                        }

                        void Write(int count)
                        {
                            var writeTask = _stream.WriteAsync(buffer, 0, count);
                            writeTask.Wait();

                            if (writeTask.IsFaulted)
                            {
                                context.Log?.Log(LogType.Exception, LogLevel.Important, () => $"Connection failed to write to Tcp stream. {writeTask.Exception.Message}");
                                throw new ConnectionException(this, "Tcp write task faulted", writeTask.Exception);
                            }

                            if (writeTask.IsCanceled)
                            {
                                context.Log?.Log(LogType.TcpIp, LogLevel.Important, () => $"Timeout writing to Tcp stream");
                                throw new ConnectionException(this, "Tcp write task timed out");
                            }
                        }

                        while (true)
                        {
                            var bytesRead = Read();
                            if (bytesRead == 0) break;

                            context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"Read {bytesRead} bytes of content from the incomming stream");

                            Write(bytesRead);

                            context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"Wrote {bytesRead} bytes to the Tcp connection");
                        }
                    }
                    finally
                    {
                        _bufferPool.Reuse(buffer);
                    }
                }
            });
        }

        private Task ReceiveHttp(IRequestContext context, int readTimeoutMs)
        {
            var hasNoContent = 
                string.Equals(context.Incoming.Method, "HEAD", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(context.Incoming.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase);

            int? contentLength = hasNoContent ? (int?)0 : null;
            var contentBytesReceived = 0;

            var line = new StringBuilder();
            var headerLines = new List<string>();

            var header = true;
            var beginning = true;

            int AppendHeader(byte[] buffer, int bytesRead)
            {
                for (var i = 0; i < bytesRead; i++)
                {
                    var c = (char)buffer[i];

                    if (beginning && c != 'H') continue;
                    if (c == '\r') continue;

                    if (c == '\n')
                    {
                        if (beginning) continue;

                        if (line.Length == 0)
                        {
                            header = false;
                            ParseHeaders();
                            return i+1;
                        }

                        headerLines.Add(line.ToString());
                        line.Clear();
                        continue;
                    }

                    line.Append(c);
                    beginning = false;
                }
                return 0;
            }

            void ParseHeaders()
            {
                context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"First blank line received, parsing headers");
                for (var j = 0; j < headerLines.Count; j++)
                    context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"> {headerLines[j]}");
                
                var firstSpaceIndex = headerLines[0].IndexOf(' ');
                context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"First space in first header line at {firstSpaceIndex}");

                if (firstSpaceIndex < 1)
                    throw new ConnectionException(this, "response first line contains no spaces");

                var secondSpaceIndex = headerLines[0].IndexOf(' ', firstSpaceIndex + 1);
                context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"Second space in first header line at {secondSpaceIndex}");

                if (secondSpaceIndex < 3)
                {
                    context.Log?.Log(LogType.TcpIp, LogLevel.Standard, () => "Response first line contains only 1 space");
                    throw new ConnectionException(this, "response first line contains only 1 space");
                }

                var statusString = headerLines[0].Substring(firstSpaceIndex + 1, secondSpaceIndex - firstSpaceIndex);
                if (ushort.TryParse(statusString, out var statusCode))
                {
                    context.Outgoing.StatusCode = statusCode;
                }
                else
                {
                    context.Log?.Log(LogType.TcpIp, LogLevel.Standard, () => $"Response status code '{statusString}' can not be parsed as ushort");
                    throw new ConnectionException(this, "response status code can not be parsed as an integer");
                }

                context.Outgoing.ReasonPhrase = headerLines[0].Substring(secondSpaceIndex + 1);

                for (var j = 1; j < headerLines.Count; j++)
                {
                    var headerLine = headerLines[j];

                    var colonPos = headerLine.IndexOf(':');
                    if (colonPos > 0)
                    {
                        var name = headerLine.Substring(0, colonPos).Trim();
                        var value = headerLine.Substring(colonPos + 1).Trim();

                        if (context.Outgoing.Headers.ContainsKey(name))
                        {
                            var originalHeaders = context.Outgoing.Headers[name];
                            var newHeaders = new string[originalHeaders.Length + 1];
                            Array.Copy(originalHeaders, 0, newHeaders, 0, originalHeaders.Length);
                            newHeaders[originalHeaders.Length] = value;
                            context.Outgoing.Headers[name] = newHeaders;
                        }
                        else
                        {
                            context.Outgoing.Headers[name] = new[] { value };
                        }

                        if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"Content length header = {value}");
                            if (int.TryParse(value, out var i))
                                contentLength = i;
                            else
                                context.Log?.Log(LogType.TcpIp, LogLevel.Standard, () => $"Content length header '{value}' is not an integer");
                        }
                    }
                    else
                    {
                        context.Log?.Log(LogType.TcpIp, LogLevel.Standard, () => $"Invalid header line '{headerLine}' does not contain a colon");
                        throw new ConnectionException(this, "header line does not contain a colon");
                    }
                }

                context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"Sending {headerLines.Count} headers in outgoing message");
                context.Outgoing.SendHeaders(context);
            }

            int Read(byte[] buffer)
            {
                context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => "Waiting for server response");

                int bytesRead;
                try
                {
                    bytesRead = _stream.Read(buffer, 0, buffer.Length);
                }
                catch (IOException)
                {
                    if (header || contentLength.HasValue)
                    {
                        context.Log?.Log(LogType.TcpIp, LogLevel.Standard, () => $"Server did not respond within {_tcpClient.ReceiveTimeout}ms");

                        context.Outgoing.StatusCode = 504;
                        context.Outgoing.ReasonPhrase = "Server did not respond within " + _tcpClient.ReceiveTimeout + "ms";
                        Dispose();
                    }

                    return 0;
                }

                return bytesRead;
            }

            void Write(byte[] buffer, int start, int count)
            {
                context.Log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"Writing {count} bytes to the outgoing message");

                contentBytesReceived += count;

                var writeTask = context.Outgoing.Content.WriteAsync(buffer, start, count);
                writeTask.Wait();

                if (writeTask.IsFaulted)
                {
                    context.Log?.Log(LogType.TcpIp, LogLevel.Important, () => $"Outgoing write task faulted '{writeTask.Exception}'");
                    throw new ConnectionException(this, "Outgoing write task faulted", writeTask.Exception);
                }

                if (writeTask.IsCanceled)
                {
                    context.Log?.Log(LogType.TcpIp, LogLevel.Important, () => $"Outgoing write task timed out");
                    throw new ConnectionException(this, "Outgoing write task timed out");
                }
            }

            context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => "Starting http receive. Expecting " + (hasNoContent ? "no content" : "possible content"));

            return Task.Run(() =>
            {
                var buffer = _bufferPool.Get();
                try
                {
                    while (header || !contentLength.HasValue || contentBytesReceived < contentLength.Value)
                    {
                        var bytesRead = Read(buffer);

                        context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"Read {bytesRead} bytes from connection stream");

                        if (bytesRead == 0)
                        {
                            if (State == ConnectionState.Busy)
                                State = ConnectionState.Connected;

                            break;
                        }

                        if (header)
                        {
                            var contentStart = AppendHeader(buffer, bytesRead);
                            if (!header)
                            {
                                if (contentStart < bytesRead)
                                    Write(buffer, contentStart, bytesRead - contentStart);

                                if (!contentLength.HasValue)
                                {
                                    context.Log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"Setting receive timeout to {readTimeoutMs}ms");
                                    _tcpClient.ReceiveTimeout = readTimeoutMs;
                                }
                            }
                        }
                        else
                        {
                            Write(buffer, 0, bytesRead);
                        }
                    }
                    context.Log?.Log(LogType.TcpIp, LogLevel.Standard, () => "Finished http receive");
                }

                finally
                {
                    _bufferPool.Reuse(buffer);
                }
            });
        }
    }
}