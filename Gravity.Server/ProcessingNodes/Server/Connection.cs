using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Gravity.Server.ProcessingNodes.Server
{
    internal class Connection: IDisposable
    {
        private readonly TimeSpan _responseTimeout;
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;

        public Connection(
            IPEndPoint endpoint,
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
        }

        public void Dispose()
        {
            _stream.Close();
            _tcpClient.Close();
        }

        public bool IsConnected
        {
            get
            {
                return _tcpClient.Connected;
            }
        }

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
            var buffer = new byte[65535];
            var contentLength = 0;
            var contentIndex = 0;
            var header = true;
            var beginning = true;

            Action parseHeaders = () =>
            {
                var firstSpaceIndex = headerLines[0].IndexOf(' ');
                var secondSpaceIndex = headerLines[0].IndexOf(' ', firstSpaceIndex + 1);
                result.StatusCode = Int32.Parse(headerLines[0].Substring(firstSpaceIndex + 1, secondSpaceIndex - firstSpaceIndex));
                result.ReasonPhrase = headerLines[0].Substring(secondSpaceIndex + 1);

                result.Headers = new Tuple<string, string>[headerLines.Count - 1];
                for (var j = 1; j < headerLines.Count; j++)
                {
                    var headerLine = headerLines[j];

                    var colonPos = headerLine.IndexOf(':');
                    var name = headerLine.Substring(0, colonPos).Trim();
                    var value = headerLine.Substring(colonPos + 1).Trim();
                    result.Headers[j - 1] = new Tuple<string, string>(name, value);

                    if (String.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                        contentLength = Int32.Parse(value);
                }

                if (contentLength > 0) result.Content = new byte[contentLength];
            };

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
                                parseHeaders();
                                header = false;

                                if (i < readCount - 1)
                                {
                                    contentIndex = readCount - i - 1;
                                    Array.Copy(buffer, i + 1, result.Content, 0, contentIndex);
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
                    if (result.Content != null)
                    {
                        Array.Copy(buffer, 0, result.Content, contentIndex, readCount);
                        contentIndex += readCount;
                    }
                }

                if (readCount < buffer.Length)
                    break;
            }
            return result;
        }
    }
}