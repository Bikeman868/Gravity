using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace Gravity.Server.ProcessingNodes.Server
{
    internal class Connection: IDisposable
    {
        private readonly TimeSpan _responseTimeout;
        private readonly TcpClient _tcpClient;
        private readonly Stream _stream;

        public Connection(
            IPEndPoint endpoint,
            string hostName,
            string protocol,
            TimeSpan connectionTimeout, 
            TimeSpan responseTimeout)
        {
            _responseTimeout = responseTimeout;

            _tcpClient = new TcpClient
            {
                ReceiveTimeout = (int)responseTimeout.TotalMilliseconds,
                SendTimeout = 0
            };

            var connectResult = _tcpClient.BeginConnect(endpoint.Address, endpoint.Port, null, null);
            if (!connectResult.AsyncWaitHandle.WaitOne(connectionTimeout))
                throw new Exception("Failed to connect within " + connectionTimeout);
            _tcpClient.EndConnect(connectResult);            

            _stream = _tcpClient.GetStream();

            if (protocol == "https")
            {
                var  sslStream = new SslStream(_stream);
                _stream = sslStream;
                sslStream.AuthenticateAsClient(hostName);
            }
        }

        public void Dispose()
        {
            _stream?.Close();
            _tcpClient?.Close();
        }

        public bool IsConnected => _tcpClient.Connected;

        public Response Send(Request request)
        {
            SendHttp(request);
            return ReceiveHttp();
        }

        private void SendHttp(Request request)
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
            _stream.Write(bytes, 0, bytes.Length);

            if (request.Content != null && request.Content.Length > 0)
                _stream.Write(request.Content, 0, request.Content.Length);
        }

        private Response ReceiveHttp()
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
                var firstSpaceIndex = headerLines[0].IndexOf(' ');
                if (firstSpaceIndex < 1) throw new Exception("Response first line contains no spaces");

                var secondSpaceIndex = headerLines[0].IndexOf(' ', firstSpaceIndex + 1);
                if (secondSpaceIndex < 3) throw new Exception("Response first line contains only 1 space");

                if (!Int32.TryParse(headerLines[0].Substring(firstSpaceIndex + 1, secondSpaceIndex - firstSpaceIndex), out result.StatusCode))
                    throw new Exception("Response status code can not be parsed as an integer");

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
                            contentLength = Int32.Parse(value);
                            fixedLength = true;
                        }
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

                if (readCount < buffer.Length)
                    break;
            }
            return result;
        }
    }
}