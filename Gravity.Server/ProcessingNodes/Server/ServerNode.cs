using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Server
{
    internal class ServerNode: INode
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public string Host { get; set; }
        public int? Port { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }
        public TimeSpan ResponseTimeout { get; set; }
        public string HealthCheckMethod { get; set; }
        public string HealthCheckHost { get; set; }
        public int HealthCheckPort { get; set; }
        public string HealthCheckPath { get; set; }
        public int[] HealthCheckCodes { get; set; }
        public TimeSpan DnsLookupInterval { get; set; }

        public bool? Healthy { get; private set; }
        public string UnhealthyReason { get; private set; }
        public bool Offline { get; private set; }

        public ServerIpAddress[] IpAddresses;
        private readonly Dictionary<IPEndPoint, ConnectionPool> _endpoints;

        private readonly Thread _heathCheckThread;
        private DateTime _nextDnsLookup;
        private int _lastIpAddressIndex;

        public ServerNode()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(5);
            ResponseTimeout = TimeSpan.FromMinutes(1);
            DnsLookupInterval = TimeSpan.FromSeconds(5);
            HealthCheckPort = 80;
            HealthCheckMethod = "GET";
            HealthCheckPath = "/";
            HealthCheckCodes = new[] { 200 };

            _endpoints = new Dictionary<IPEndPoint, ConnectionPool>();

            _heathCheckThread = new Thread(() =>
            {
                var counter = 0;
                while (true)
                {
                    try
                    {
                        Thread.Sleep(1000);

                        if (!Disabled && !string.IsNullOrEmpty(Host))
                            CheckHealth();

                        if (++counter == 5)
                        {
                            counter = 0;
                            var ipAddresses = IpAddresses;
                            if (ipAddresses != null)
                            {
                                for (var i = 0; i < ipAddresses.Length; i++)
                                {
                                    ipAddresses[i].TrafficAnalytics.Recalculate();
                                }
                            }
                        }
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

        void INode.UpdateStatus()
        {
            if (Disabled || IpAddresses == null)
            {
                Offline = true;
                return;
            }

            Offline = Healthy == false;
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            var allIpAddresses = IpAddresses;

            if (allIpAddresses == null || allIpAddresses.Length == 0)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "No servers found with this host name";
                return context.Response.WriteAsync(string.Empty);
            }

            var ipAddresses = allIpAddresses.Where(i => i.Healthy == true).ToList();

            if (ipAddresses.Count == 0 || Healthy != true)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "No healthy servers";
                return context.Response.WriteAsync(string.Empty);
            }

            var ipAddressIndex = Interlocked.Increment(ref _lastIpAddressIndex) % ipAddresses.Count;
            var ipAddress = ipAddresses[ipAddressIndex];

            var port = 80;
            if (Port.HasValue)
            {
                port = Port.Value;
            }
            else if (string.Equals(context.Request.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                port = 443;
            }

            var request = new Request
            {
                IpAddress = ipAddress.Address,
                PortNumber = port,
                Method = context.Request.Method,
                PathAndQuery = context.Request.Path.ToString(),
                Headers = context.Request.Headers
                    .Where(h => h.Value != null && h.Value.Length > 0)
                    .Select(h => new Tuple<string, string>(h.Key, h.Value[0]))
                    .ToArray()
            };

            if (context.Request.QueryString.HasValue)
                request.PathAndQuery += "?" + context.Request.QueryString.Value;

            var contentLengthHeader = context.Request.Headers["Content-Length"];
            if (contentLengthHeader != null && context.Request.Body != null && context.Request.Body.CanRead)
            {
                var contentLength = int.Parse(contentLengthHeader);
                request.Content = new byte[contentLength];
                context.Request.Body.Read(request.Content, 0, contentLength);
            }

            Response response;
            ipAddress.IncrementConnectionCount();
            var startTime = ipAddress.TrafficAnalytics.BeginRequest();
            try
            {
                response = Send(request);
            }
            finally
            {
                ipAddress.TrafficAnalytics.EndRequest(startTime);
                ipAddress.DecrementConnectionCount();
            }

            context.Response.StatusCode = response.StatusCode;
            context.Response.ReasonPhrase = response.ReasonPhrase;

            if (response.Headers != null)
                foreach (var header in response.Headers)
                    context.Response.Headers[header.Item1] = header.Item2;

            return context.Response.WriteAsync(response.Content);
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
                        var newIpAddresses = hostEntry.AddressList
                                .Select(a => new ServerIpAddress {Address = a})
                                .ToArray();

                        if (IpAddresses == null)
                        {
                            IpAddresses = newIpAddresses;
                        }
                        else
                        {
                            if (IpAddresses.Length == newIpAddresses.Length)
                            {
                                for (var i = 0; i < IpAddresses.Length; i++)
                                {
                                    if (!IpAddresses[i].Address.Equals(newIpAddresses[i].Address))
                                    {
                                        IpAddresses[i] = newIpAddresses[i];
                                    }
                                }
                            }
                            else
                            {
                                IpAddresses = newIpAddresses;
                            }
                        }
                        _nextDnsLookup = DateTime.UtcNow + DnsLookupInterval;
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
                try
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

                    if (HealthCheckCodes.Contains(response.StatusCode))
                    {
                        healthy = true;
                        IpAddresses[i].SetHealthy();
                    }
                    else
                    {
                        IpAddresses[i].SetUnhealthy("Status code " + response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    IpAddresses[i].SetUnhealthy(ex.Message);
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
            try
            {
                var endpoint = new IPEndPoint(request.IpAddress, request.PortNumber);

                ConnectionPool connectionPool;
                lock (_endpoints)
                {
                    if (!_endpoints.TryGetValue(endpoint, out connectionPool))
                    {
                        connectionPool = new ConnectionPool(endpoint, ConnectionTimeout, ResponseTimeout);
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
            catch (Exception ex)
            {
                return new Response
                {
                    StatusCode = 503,
                    ReasonPhrase = "Exception forwarding request to real server",
                    Headers = new[]
                    {
                        new Tuple<string, string>("Retry-After", "60"),
                        new Tuple<string, string>("X-Exception", ex.Message)
                    }
                };
            }
        }
    }
}