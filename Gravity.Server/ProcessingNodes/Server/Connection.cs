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
        private int _readTimeoutMs;
        private DateTime _lastUsedUtc;
        private readonly TimeSpan _maximumIdleTime = TimeSpan.FromMinutes(5);

        public Connection(
            ILog log,
            IPEndPoint endpoint,
            string hostName,
            string protocol,
            TimeSpan connectionTimeout)
        {
            log?.Log(LogType.TcpIp, LogLevel.Standard, 
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
                log?.Log(LogType.Exception, LogLevel.Important, () => $"Failed to connect within {connectionTimeout}");
                _tcpClient.Close();
                throw new Exception("Failed to connect within " + connectionTimeout);
            }

            _tcpClient.EndConnect(connectResult);            

            _stream = _tcpClient.GetStream();

            if (protocol == "https")
            {
                log?.Log(LogType.TcpIp, LogLevel.Standard, () => "Wrapping Tcp connection in SSL stream");
                var sslStream = new SslStream(_stream);

                _stream = sslStream;

                log?.Log(LogType.TcpIp, LogLevel.Standard, () => $"Authenticating server's SSL certificate for {hostName}");
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
        public Connection Initialize(TimeSpan responseTimeout, int readTimeoutMs)
        {
            _responseTimeout = responseTimeout;
            _readTimeoutMs = readTimeoutMs;

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
                    log?.Log(LogType.TcpIp, LogLevel.Standard, () => "Response first line contains only 1 space");
                    throw new Exception("Response first line contains only 1 space");
                }

                if (!int.TryParse(headerLines[0].Substring(firstSpaceIndex + 1, secondSpaceIndex - firstSpaceIndex), out result.StatusCode))
                {
                    log?.Log(LogType.TcpIp, LogLevel.Standard, () => "Response status code can not be parsed as an integer");
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
                        log?.Log(LogType.TcpIp, LogLevel.Standard, () => $"Invalid header line '{headerLine}' does not contain a colon");
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

            log?.Log(LogType.TcpIp, LogLevel.Detailed, () => "Starting http receive. Expecting " + (expectBody ? "possible body" : "no body"));

            _tcpClient.ReceiveTimeout = (int)_responseTimeout.TotalMilliseconds;

            while (header || !contentLength.HasValue || (contentLength.Value > 0 && (result.Content == null || result.Content.Length < contentLength.Value)))
            {
                log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"Waiting up to {_tcpClient.ReceiveTimeout}ms for a response from the server");
                int readCount;
                try
                {
                    readCount = _stream.Read(buffer, 0, buffer.Length);
                }
                catch (IOException)
                {
                    log?.Log(LogType.TcpIp, LogLevel.Standard, () => $"Server did not respond within {_tcpClient.ReceiveTimeout}ms");
                    if (contentLength.HasValue || header)
                    {
                        result.StatusCode = 504;
                        result.ReasonPhrase = "Server did not respond within " + _tcpClient.ReceiveTimeout + "ms";
                        Dispose();
                    }
                    readCount = 0;
                }
                if (readCount == 0) break;

                log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () =>
                {
                    var msg = $"{readCount} bytes received from the server";
                    if (readCount < 30) 
                        msg += " '" + Encoding.UTF8.GetString(buffer, 0, readCount)  + "'";
                    return msg;
                });

                if (header)
                {
                    for (var i = 0; i < readCount; i++)
                    {
                        var c = (char) buffer[i];

                        if (beginning && c != 'H') continue;

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

                    _tcpClient.ReceiveTimeout = _readTimeoutMs;
                }
            }

            if (!expectBody && result.Content != null && result.Content.Length > 0)
            {
                var msg = $"No content body was expected but {result.Content.Length} bytes of content were received";
                log?.Log(LogType.TcpIp, LogLevel.Important, () => msg);
                throw new Exception(msg);
            }

            log?.Log(LogType.TcpIp, LogLevel.Standard, () =>
            {
                var headers = result.Headers == null ? "no headers" : $"{result.Headers.Length} header lines";
                var content = result.Content == null ? "no content" : $"{result.Content.Length} bytes of content";
                return $"Response status {result.StatusCode} '{result.ReasonPhrase}' with {content} and {headers}";
            });

            return result;
        }
    }
}