using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using Gravity.Server.Interfaces;

namespace Gravity.Server.ProcessingNodes.Server
{
    internal class Connection: IDisposable
    {
        private readonly TcpClient _tcpClient;
        private readonly Stream _stream;

        private TimeSpan _responseTimeout;
        private TimeSpan _readTimeout;
        private DateTime _lastUsedUtc;
        private readonly TimeSpan _maximumIdleTime = TimeSpan.FromMinutes(5);

        public Connection(
            ILog log,
            IPEndPoint endpoint,
            string hostName,
            string protocol,
            TimeSpan connectionTimeout)
        {
            log?.Log(LogType.TcpIp, LogLevel.Basic, 
                () => $"Opening a new Tcp connection to {protocol}://{hostName} at {endpoint}");


            _tcpClient = new TcpClient
            {
                ReceiveTimeout = 0,
                SendTimeout = 0,
                LingerState = new LingerOption(true, 10),
                NoDelay = true
            };

            var connectResult = _tcpClient.BeginConnect(endpoint.Address, endpoint.Port, null, null);
            if (!connectResult.AsyncWaitHandle.WaitOne(connectionTimeout))
            {
                log?.Log(LogType.Exception, LogLevel.Superficial, () => $"Failed to connect within {connectionTimeout}");
                _tcpClient.Close();
                throw new Exception("Failed to connect within " + connectionTimeout);
            }

            _tcpClient.EndConnect(connectResult);            

            _stream = _tcpClient.GetStream();

            if (protocol == "https")
            {
                log?.Log(LogType.TcpIp, LogLevel.Basic, () => "Wrapping Tcp connection in SSL stream");
                var sslStream = new SslStream(_stream);

                _stream = sslStream;

                log?.Log(LogType.TcpIp, LogLevel.Basic, () => $"Authenticating server's SSL certificate for {hostName}");
                sslStream.AuthenticateAsClient(hostName);
            }

            _lastUsedUtc = DateTime.UtcNow;
        }

        public void Dispose()
        {
            _stream?.Close();
            _tcpClient?.Close();
        }

        /// <summary>
        /// Gets this connection ready for a new request
        /// </summary>
        /// <returns></returns>
        public Connection Initialize(TimeSpan responseTimeout, TimeSpan readTimeout)
        {
            _responseTimeout = responseTimeout;
            _readTimeout = readTimeout;

            return this;
        }

        public bool IsConnected => _tcpClient.Connected;

        public bool IsStale => (DateTime.UtcNow - _lastUsedUtc) > _maximumIdleTime;

        public Response Send(Request request, ILog log)
        {
            SendHttp(request, log);

            var expectBodyInResponse = !string.Equals(request.Method, "HEAD");
            var response = ReceiveHttp(expectBodyInResponse, log);

            _lastUsedUtc = DateTime.UtcNow;

            return response;
        }

        private void SendHttp(Request request, ILog log)
        {
            var buffer = new StringBuilder();

            buffer.Append(request.Method);
            buffer.Append(' ');
            buffer.Append(request.Protocol);
            buffer.Append("://");
            buffer.Append(request.HostName);
            buffer.Append(':');
            buffer.Append(request.PortNumber);
            buffer.Append(request.PathAndQuery);
            buffer.Append(' ');
            buffer.Append("HTTP/1.1");
            buffer.Append("\r\n");

            if (request.Headers != null)
            {
                foreach (var header in request.Headers)
                {
                    if (string.Equals("Connection", header.Item1, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (string.Equals("Keep-Alive", header.Item1, StringComparison.OrdinalIgnoreCase))
                        continue;

                    buffer.Append(header.Item1);
                    buffer.Append(": ");
                    buffer.Append(header.Item2);
                    buffer.Append("\r\n");
                }
            }
            buffer.Append("Connection: Keep-Alive\r\n");
            buffer.Append("Keep-Alive: timeout=");
            buffer.Append((int)(_maximumIdleTime.TotalSeconds + 5));
            buffer.Append("\r\n\r\n");

            var bytes = Encoding.ASCII.GetBytes(buffer.ToString());

            log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"Writing {bytes.Length} bytes of header to the connection stream");
            _stream.Write(bytes, 0, bytes.Length);

            if (request.Content != null && request.Content.Length > 0)
            {
                log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"Writing {request.Content.Length} bytes of content body to the connection stream");
                _stream.Write(request.Content, 0, request.Content.Length);
            }
        }

        private Response ReceiveHttp(bool expectBody, ILog log)
        {
            var result = new Response
            {
                StatusCode = 444,
                ReasonPhrase = "Invalid response"
            };

            var line = new StringBuilder();
            var headerLines = new List<string>();
            var buffer = new byte[50000];
            int? contentLength = expectBody ? null : (int?)0;
            var contentIndex = 0;
            var header = true;
            var beginning = true;

            void ParseHeaders()
            {
                log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"First blank line received, parsing headers");
                for (var j = 0; j < headerLines.Count; j++)
                    log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"> {headerLines[j]}");
                
                var firstSpaceIndex = headerLines[0].IndexOf(' ');
                log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"First space in first header line at {firstSpaceIndex}");

                if (firstSpaceIndex < 1)
                    throw new Exception("Response first line contains no spaces");

                var secondSpaceIndex = headerLines[0].IndexOf(' ', firstSpaceIndex + 1);
                log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"Second space in first header line at {secondSpaceIndex}");

                if (secondSpaceIndex < 3)
                {
                    log?.Log(LogType.TcpIp, LogLevel.Basic, () => "Response first line contains only 1 space");
                    throw new Exception("Response first line contains only 1 space");
                }

                if (!int.TryParse(headerLines[0].Substring(firstSpaceIndex + 1, secondSpaceIndex - firstSpaceIndex), out result.StatusCode))
                {
                    log?.Log(LogType.TcpIp, LogLevel.Basic, () => "Response status code can not be parsed as an integer");
                    throw new Exception("Response status code can not be parsed as an integer");
                }

                result.ReasonPhrase = headerLines[0].Substring(secondSpaceIndex + 1);

                result.Headers = new Tuple<string, string>[headerLines.Count - 1];

                for (var j = 1; j < headerLines.Count; j++)
                {
                    var headerLine = headerLines[j];

                    var colonPos = headerLine.IndexOf(':');
                    if (colonPos > 0)
                    {
                        var name = headerLine.Substring(0, colonPos).Trim();
                        var value = headerLine.Substring(colonPos + 1).Trim();
                        result.Headers[j - 1] = new Tuple<string, string>(name, value);

                        if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                        {
                            log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"Content length header = {value}");
                            contentLength = int.Parse(value);
                        }
                    }
                    else
                    {
                        log?.Log(LogType.TcpIp, LogLevel.Basic, () => $"Invalid header line '{headerLine}' does not contain a colon");
                        throw new Exception("Invalid header line does not contain a colon");
                    }
                }
            }

            void AppendContent(int src, int dest, int count)
            {
                if (result.Content == null)
                {
#if DEBUG
                    if (dest != 0) throw new Exception("Destination offset for copy must be 0 when the content buffer has not been allocated yet");
#endif
                    result.Content = new byte[contentLength ?? count];
                }
                else if (result.Content.Length < dest + count)
                {
                    var newContent = new byte[dest + count];
                    Array.Copy(result.Content, 0, newContent, 0, result.Content.Length);
                    result.Content = newContent;
                }

                Array.Copy(buffer, src, result.Content, dest, count);
            }

            log?.Log(LogType.TcpIp, LogLevel.Detailed, () => "Waiting for a response from the server");

            _tcpClient.ReceiveTimeout = (int)_responseTimeout.TotalMilliseconds;
            while (header || (contentLength.HasValue && contentLength.Value > 0 && (result.Content == null || result.Content.Length < contentLength.Value)))
            {
                int readCount;
                try
                {
                    readCount = _stream.Read(buffer, 0, buffer.Length);
                }
                catch (IOException)
                {
                    log?.Log(LogType.TcpIp, LogLevel.Basic, () => $"Server did not respond within {_tcpClient.ReceiveTimeout}ms");
                    result.StatusCode = 504;
                    result.ReasonPhrase = "Server did not respond within " + _tcpClient.ReceiveTimeout + "ms";
                    Dispose();
                    return result;
                }
                if (readCount == 0) break;
                _tcpClient.ReceiveTimeout = (int)_readTimeout.TotalMilliseconds;

                log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"{readCount} bytes received from the server");

                if (header)
                {
                    for (var i = 0; i < readCount; i++)
                    {
                        var c = (char) buffer[i];

                        if (c == '\r') continue;

                        if (c == '\n')
                        {
                            if (beginning) continue;

                            if (line.Length == 0)
                            {
                                ParseHeaders();
                                header = false;

                                if (i < readCount - 1)
                                {
                                    contentIndex = readCount - i - 1;
                                    AppendContent(i + 1, 0, contentIndex);
                                }
                                break;
                            }

                            headerLines.Add(line.ToString());
                            line.Clear();
                            continue;
                        }

                        line.Append(c);
                        beginning = false;
                    }
                }
                else
                {
                    AppendContent(0, contentIndex, readCount);
                    contentIndex += readCount;
                }
            }

            log?.Log(LogType.TcpIp, LogLevel.Basic, () =>
            {
                var headers = result.Headers == null ? "no headers" : $"{result.Headers.Length} header lines";
                var content = result.Content == null ? "no content" : $"{result.Content.Length} bytes of content";
                return $"Response status {result.StatusCode} '{result.ReasonPhrase}' with {content} and {headers}";
            });

            return result;
        }
    }
}