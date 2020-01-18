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
        private readonly TimeSpan _responseTimeout;
        private readonly TcpClient _tcpClient;
        private readonly Stream _stream;

        public Connection(
            ILog log,
            IPEndPoint endpoint,
            string hostName,
            string protocol,
            TimeSpan connectionTimeout, 
            TimeSpan responseTimeout)
        {
            log?.Log(LogType.TcpIp, LogLevel.Basic, 
                () => $"Opening a new Tcp connection to {protocol}://{hostName} at {endpoint}");

            _responseTimeout = responseTimeout;

            _tcpClient = new TcpClient
            {
                ReceiveTimeout = (int)responseTimeout.TotalMilliseconds,
                SendTimeout = 0,
                LingerState = new LingerOption(true, 10),
                NoDelay = true
            };

            //_tcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.KeepAlive, 1);

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
        }

        public void Dispose()
        {
            _stream?.Close();
            _tcpClient?.Close();
        }

        public bool IsConnected => _tcpClient.Connected;

        public Response Send(Request request, ILog log)
        {
            SendHttp(request, log);
            return ReceiveHttp(log);
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
                    buffer.Append(header.Item1);
                    buffer.Append(": ");
                    buffer.Append(header.Item2);
                    buffer.Append("\r\n");
                }
            }

            buffer.Append("\r\n");

            var bytes = Encoding.ASCII.GetBytes(buffer.ToString());

            log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"Writing {bytes.Length} bytes of header to the connection stream");
            _stream.Write(bytes, 0, bytes.Length);

            if (request.Content != null && request.Content.Length > 0)
            {
                log?.Log(LogType.TcpIp, LogLevel.Detailed, () => $"Writing {request.Content.Length} bytes of content body to the connection stream");
                _stream.Write(request.Content, 0, request.Content.Length);
            }
        }

        private Response ReceiveHttp(ILog log)
        {
            var result = new Response
            {
                StatusCode = 444,
                ReasonPhrase = "Invalid response"
            };

            var line = new StringBuilder();
            var headerLines = new List<string>();
            var buffer = new byte[50000];
            var contentLength = 0;
            var contentIndex = 0;
            var header = true;
            var beginning = true;
            var fixedLength = false;

            void ParseHeaders()
            {
                log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"Parsing {headerLines.Count} header lines. First line is '{headerLines[0]}'");

                var firstSpaceIndex = headerLines[0].IndexOf(' ');
                log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"First space in first header line at {firstSpaceIndex}");

                if (firstSpaceIndex < 1)
                    throw new Exception("Response first line contains no spaces");

                var secondSpaceIndex = headerLines[0].IndexOf(' ', firstSpaceIndex + 1);
                log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"Second space in first header line at {secondSpaceIndex}");

                if (secondSpaceIndex < 3)
                    throw new Exception("Response first line contains only 1 space");

                if (!int.TryParse(headerLines[0].Substring(firstSpaceIndex + 1, secondSpaceIndex - firstSpaceIndex), out result.StatusCode))
                    throw new Exception("Response status code can not be parsed as an integer");

                result.ReasonPhrase = headerLines[0].Substring(secondSpaceIndex + 1);

                result.Headers = new Tuple<string, string>[headerLines.Count - 1];

                for (var j = 1; j < headerLines.Count; j++)
                {
                    var headerLine = headerLines[j];
                    log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"Header line {j} = '{headerLine}'");

                    var colonPos = headerLine.IndexOf(':');
                    if (colonPos > 0)
                    {
                        var name = headerLine.Substring(0, colonPos).Trim();
                        var value = headerLine.Substring(colonPos + 1).Trim();
                        result.Headers[j - 1] = new Tuple<string, string>(name, value);

                        if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                        {
                            log?.Log(LogType.TcpIp, LogLevel.VeryDetailed, () => $"Content length header is {value}");
                            contentLength = int.Parse(value);
                            fixedLength = true;
                        }
                    }
                    else
                    {
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
                    result.Content = new byte[fixedLength ? contentLength : count];
                }
                else if (result.Content.Length < dest + count)
                {
                    var newContent = new byte[dest + count];
                    Array.Copy(result.Content, 0, newContent, 0, result.Content.Length);
                    result.Content = newContent;
                }

                Array.Copy(buffer, src, result.Content, dest, count);
            }

            while (true)
            {
                int readCount;
                try
                {
                    readCount = _stream.Read(buffer, 0, buffer.Length);
                }
                catch (IOException)
                {
                    result.StatusCode = 504;
                    result.ReasonPhrase = "Server did not respond within " + _responseTimeout;
                    Dispose();
                    return result;
                }

                if (readCount == 0) break;

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

            log?.Log(LogType.TcpIp, LogLevel.Basic, () => $"Received {result.StatusCode} response with {result.Content?.Length ?? 0} bytes of content and {result.Headers?.Length} header lines");

            return result;
        }
    }
}