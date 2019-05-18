using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.DataStructures;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes
{
    internal class ServerNode: INode
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }
        public TimeSpan RequestTimeout { get; set; }
        public string HealthCheckMethod { get; set; }
        public string HealthCheckHost { get; set; }
        public int HealthCheckPort { get; set; }
        public string HealthCheckPath { get; set; }

        public bool? Healthy { get; private set; }
        public string UnhealthyReason { get; private set; }

        public ServerIpAddress[] IpAddresses;
        private readonly Dictionary<IPEndPoint, ConnectionPool> _endpoints;

        private readonly Thread _heathCheckThread;
        private DateTime _nextDnsLookup;

        public ServerNode()
        {
            Port = 80;
            ConnectionTimeout = TimeSpan.FromSeconds(5);
            RequestTimeout = TimeSpan.FromMinutes(1);

            HealthCheckPort = 80;
            HealthCheckMethod = "GET";
            HealthCheckPath = "/";

            _endpoints = new Dictionary<IPEndPoint, ConnectionPool>();

            _heathCheckThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        Thread.Sleep(1000);
                        if (!Disabled && !string.IsNullOrEmpty(Host))
                            CheckHealth();
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        UnhealthyReason = ex.Message;
                        Healthy = false;
                    }
                }
            })
            {
                Name = "Server health check",
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
        }

        public void Dispose()
        {
            _heathCheckThread.Abort();
            _heathCheckThread.Join(TimeSpan.FromSeconds(10));

            lock (_endpoints)
            {
                foreach (var endpoint in _endpoints.Values)
                    endpoint.Dispose();
                _endpoints.Clear();
            }
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
            _heathCheckThread.Start();
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            if (Healthy.HasValue && Healthy.Value)
            {
                context.Response.StatusCode = 200;
                context.Response.ReasonPhrase = "OK";
                return context.Response.WriteAsync(Name);
            }

            context.Response.StatusCode = 503;
            context.Response.ReasonPhrase = "OK";
            return context.Response.WriteAsync(Name);
        }

        private void CheckHealth()
        {
            if (IpAddresses == null || DateTime.UtcNow > _nextDnsLookup)
            {
                IPAddress ipAddress;

                if (IPAddress.TryParse(Host, out ipAddress))
                {
                    IpAddresses = new[]
                    {
                        new ServerIpAddress
                        {
                            Address = ipAddress
                        }
                    };
                    _nextDnsLookup = DateTime.UtcNow.AddHours(1);
                }
                else
                {
                    try
                    {
                        var hostEntry = Dns.GetHostEntry(Host);
                        if (hostEntry.AddressList == null || hostEntry.AddressList.Length == 0)
                        {
                            UnhealthyReason = "DNS returned no IP addresses for " + Host;
                            Healthy = false;
                            _nextDnsLookup = DateTime.UtcNow.AddSeconds(10);
                            return;
                        }
                        IpAddresses = hostEntry.AddressList
                            .Select(a => new ServerIpAddress {Address = a})
                            .ToArray();
                        _nextDnsLookup = DateTime.UtcNow.AddMinutes(10);
                    }
                    catch (Exception ex)
                    {
                        UnhealthyReason = ex.Message + " " + Host;
                        Healthy = false;
                        _nextDnsLookup = DateTime.UtcNow.AddSeconds(10);
                        return;
                    }
                }
            }

            var host = HealthCheckHost ?? Host;
            if (HealthCheckPort != 80) host += ":" + HealthCheckPort;

            var healthy = false;

            for (var i = 0; i < IpAddresses.Length; i++)
            {
                var request = new Request
                {
                    IpAddress = IpAddresses[i].Address,
                    PortNumber = HealthCheckPort,
                    Method = HealthCheckMethod,
                    PathAndQuery = HealthCheckPath,
                    Headers = new[]
                    {
                        new Tuple<string, string>("Host", host)
                    }
                };

                var response = Send(request);

                if (response.StatusCode == 200)
                {
                    healthy = true;
                    IpAddresses[i].SetHealthy();
                }
                else
                {
                    IpAddresses[i].SetUnhealthy("Status code " + response.StatusCode);
                }
            }

            if (healthy)
                Healthy = true;
            else
            {
                UnhealthyReason = "No healthy IP addresses";
                Healthy = false;
            }
        }

        private Response Send(Request request)
        {
            var endpoint = new IPEndPoint(request.IpAddress, request.PortNumber);

            ConnectionPool connectionPool;
            lock (_endpoints)
            {
                if (!_endpoints.TryGetValue(endpoint, out connectionPool))
                {
                    connectionPool = new ConnectionPool(endpoint, ConnectionTimeout, RequestTimeout);
                    _endpoints.Add(endpoint, connectionPool);
                }
            }

            var connection = connectionPool.GetConnection();
            try
            {
                return connection.Send(request);
            }
            finally
            {
                connectionPool.ReuseConnection(connection);
            }
        }

        private class ConnectionPool: IDisposable
        {
            private readonly IPEndPoint _endpoint;
            private readonly TimeSpan _connectionTimeout;
            private readonly TimeSpan _requestTimeout;
            private readonly Queue<Connection> _pool;

            public ConnectionPool(
                IPEndPoint endpoint,
                TimeSpan connectionTimeout, 
                TimeSpan requestTimeout)
            {
                _endpoint = endpoint;
                _connectionTimeout = connectionTimeout;
                _requestTimeout = requestTimeout;
                _pool = new Queue<Connection>();
            }

            public void Dispose()
            {
                lock (_pool)
                {
                    while (_pool.Count > 0)
                        _pool.Dequeue().Dispose();
                }
            }

            public Connection GetConnection()
            {
                lock (_pool)
                {
                    if (_pool.Count > 0)
                        return _pool.Dequeue();
                }

                return new Connection(_endpoint);
            }

            public void ReuseConnection(Connection connection)
            {
                if (connection.IsConnected)
                {
                    lock (_pool)
                    {
                        if (_pool.Count < 500)
                        {
                            _pool.Enqueue(connection);
                            return;
                        }
                    }
                }

                connection.Dispose();
            }
        }

        private class Connection: IDisposable
        {
            private readonly TcpClient _tcpClient;
            private readonly NetworkStream _stream;

            public Connection(IPEndPoint endpoint)
            {
                _tcpClient = new TcpClient();
                _tcpClient.Connect(endpoint);
                _stream = _tcpClient.GetStream();
            }

            public void Dispose()
            {
                _stream.Close();
                _tcpClient.Close();
            }

            public bool IsConnected
            {
                get { return false; }
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
                //var buffer = new byte[65535];
                var buffer = new byte[10];
                var contentLength = 0;
                var contentIndex = 0;
                var header = true;
                var beginning = true;

                Action parseHeaders = () =>
                {
                    var firstSpaceIndex = headerLines[0].IndexOf(' ');
                    var secondSpaceIndex = headerLines[0].IndexOf(' ', firstSpaceIndex + 1);
                    result.StatusCode = int.Parse(headerLines[0].Substring(firstSpaceIndex + 1, secondSpaceIndex - firstSpaceIndex));
                    result.ReasonPhrase = headerLines[0].Substring(secondSpaceIndex + 1);

                    result.Headers = new Tuple<string, string>[headerLines.Count - 1];
                    for (var j = 1; j < headerLines.Count; j++)
                    {
                        var headerLine = headerLines[j];

                        var colonPos = headerLine.IndexOf(':');
                        var name = headerLine.Substring(0, colonPos).Trim();
                        var value = headerLine.Substring(colonPos + 1).Trim();
                        result.Headers[j - 1] = new Tuple<string, string>(name, value);

                        if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                            contentLength = int.Parse(value);
                    }

                    if (contentLength > 0) result.Content = new byte[contentLength];
                };

                while (true)
                {
                    var readCount = _stream.Read(buffer, 0, buffer.Length);
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

        private class Request
        {
            public IPAddress IpAddress;
            public int PortNumber;
            public string Method;
            public string PathAndQuery;
            public Tuple<string, string>[] Headers;
            public byte[] Content;
        }

        private class Response
        {
            public int StatusCode;
            public string ReasonPhrase;
            public Tuple<string, string>[] Headers;
            public byte[] Content;
        }
    }
}