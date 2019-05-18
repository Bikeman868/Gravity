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

        public bool Healthy { get; private set; }
        public string UnhealthyReason { get; private set; }

        public ServerIpAddress[] IpAddresses;
        private Dictionary<IPEndPoint, ConnectionPool> _connections;

        private readonly Thread _heathCheckThread;
        private DateTime _nextDnsLookup;

        public ServerNode()
        {
            Port = 80;
            ConnectionTimeout = TimeSpan.FromSeconds(5);
            RequestTimeout = TimeSpan.FromMinutes(1);

            Healthy = true;
            HealthCheckPort = 80;
            HealthCheckMethod = "GET";
            HealthCheckPath = "/";

            _connections = new Dictionary<IPEndPoint, ConnectionPool>();

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
            _heathCheckThread.Start();
        }

        public void Dispose()
        {
            _heathCheckThread.Abort();
            _heathCheckThread.Join(TimeSpan.FromSeconds(10));
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            if (Healthy)
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
                    IpAddress = IpAddresses[i].Address.ToString(),
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
            if (request.IpAddress == "127.0.0.1")
                return new Response
                {
                    StatusCode = 200,
                    ReasonPhrase = "OK",
                    Headers = new[]
                    {
                        new Tuple<string, string>("Content-Length", "7")
                    },
                    Content = Encoding.UTF8.GetBytes("Success")
                };

            return new Response
            {
                StatusCode = 404,
                ReasonPhrase = "Not found",
                Headers = new[]
                {
                    new Tuple<string, string>("Content-Length", "0")
                }
            };
        }

        private class ConnectionPool: IDisposable
        {
            private readonly IPEndPoint _endpoint;
            private readonly Queue<Connection> _pool;

            public ConnectionPool(IPEndPoint endpoint)
            {
                _endpoint = endpoint;
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

            void ReuseConnection(Connection connection)
            {
                lock (_pool)
                {
                    if (_pool.Count < 1000)
                    {
                        _pool.Enqueue(connection);
                        return;
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
                _tcpClient = new TcpClient(endpoint);
                _stream = _tcpClient.GetStream();
            }

            public void Dispose()
            {
                _stream.Close();
                _tcpClient.Close();
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
                buffer.Append('\n');

                if (request.Headers != null)
                {
                    foreach (var header in request.Headers)
                    {
                        buffer.Append(header.Item1);
                        buffer.Append(": ");
                        buffer.Append(header.Item2);
                        buffer.Append('\n');
                    }
                }

                buffer.Append('\n');

                var bytes = Encoding.ASCII.GetBytes(buffer.ToString());
                _stream.Write(bytes, 0, bytes.Length);

                if (request.Content != null && request.Content.Length > 0)
                    _stream.Write(request.Content, 0, request.Content.Length);
            }

            private Response ReceiveHttp()
            {
                var result = new Response
                {
                    StatusCode = 200,
                    ReasonPhrase = "",
                    Headers = new Tuple<string,string>[0],
                    Content = new byte[0]
                };


                return result;
            }
        }

        private class Request
        {
            public string IpAddress;
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