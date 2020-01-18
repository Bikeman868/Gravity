using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly Dictionary<string, ConnectionPool> _connectionPools;

        private readonly Thread _heathCheckThread;
        private DateTime _nextDnsLookup;
        private int _lastIpAddressIndex;

        public ServerNode()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(5);
            ResponseTimeout = TimeSpan.FromMinutes(1);
            DnsLookupInterval = TimeSpan.FromMinutes(5);
            HealthCheckPort = 80;
            HealthCheckMethod = "GET";
            HealthCheckPath = "/";
            HealthCheckCodes = new[] { 200 };

            _connectionPools = new Dictionary<string, ConnectionPool>();

            _heathCheckThread = new Thread(() =>
            {
                var counter = 0;
                var log = new HealthCheckLog();
                while (true)
                {
                    try
                    {
                        Thread.Sleep(3000);

                        if (!Disabled && !string.IsNullOrEmpty(Host))
                            CheckHealth(log);

                        if (++counter == 3)
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
            _heathCheckThread.Join(TimeSpan.FromSeconds(60));

            lock (_connectionPools)
            {
                foreach (var endpoint in _connectionPools.Values)
                    endpoint.Dispose();
                _connectionPools.Clear();
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

        Task INode.ProcessRequest(IOwinContext context, ILog log)
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
            var protocol = context.Request.Scheme;

            if (Port.HasValue)
            {
                port = Port.Value;
                protocol = port == 443 ? "https" : "http";
            }
            else if (string.Equals(protocol, "https", StringComparison.OrdinalIgnoreCase))
            {
                port = 443;
            }

            var host = context.Request.Headers["Host"];
            var hostColon = host.IndexOf(':');
            if (hostColon > 0)
                host = host.Substring(0, hostColon);

            var request = new Request
            {
                Protocol = protocol,
                HostName = host,
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
            var startTicks = ipAddress.TrafficAnalytics.BeginRequest();
            try
            {
                response = Send(request, log);
            }
            finally
            {
                ipAddress.TrafficAnalytics.EndRequest(startTicks);
                ipAddress.DecrementConnectionCount();
            }

            context.Response.StatusCode = response.StatusCode;
            context.Response.ReasonPhrase = response.ReasonPhrase;

            if (response.Headers != null)
                foreach (var header in response.Headers)
                    context.Response.Headers[header.Item1] = header.Item2;

            return context.Response.WriteAsync(response.Content);
        }

        private void CheckHealth(ILog log)
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
                        log.Log(LogType.Health, LogLevel.Detailed, () => $"Looking up IP address for {Host}");

                        var hostEntry = Dns.GetHostEntry(Host);

                        if (hostEntry.AddressList == null || hostEntry.AddressList.Length == 0)
                        {
                            log.Log(LogType.Health, LogLevel.Superficial, () => "DNS returned no IP addresses");

                            UnhealthyReason = "DNS returned no IP addresses for " + Host;
                            Healthy = false;
                            _nextDnsLookup = DateTime.UtcNow.AddSeconds(10);
                            return;
                        }

                        log.Log(LogType.Health, LogLevel.Detailed, () => "DNS returned " + string.Join(", ", hostEntry.AddressList.Select(a => a.ToString())));

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
                        log.Log(LogType.Exception, LogLevel.Superficial, () => "Exception in DNS lookup of " + Host + ". " + ex.Message);

                        UnhealthyReason = ex.Message + " " + Host;
                        Healthy = false;
                        _nextDnsLookup = DateTime.UtcNow.AddSeconds(10);
                        return;
                    }
                }
            }

            var host = HealthCheckHost ?? Host;
            var hostHeader = HealthCheckPort == 80 ? host : host + ":" + HealthCheckPort;

            var healthy = false;

            for (var i = 0; i < IpAddresses.Length; i++)
            {
                try
                {
                    var request = new Request
                    {
                        Protocol = HealthCheckPort == 443 ? "https" : "http",
                        HostName = host,
                        IpAddress = IpAddresses[i].Address,
                        PortNumber = HealthCheckPort,
                        Method = HealthCheckMethod,
                        PathAndQuery = HealthCheckPath,
                        Headers = new[]
                        {
                            new Tuple<string, string>("Host", hostHeader)
                        }
                    };

                    var response = Send(request, log);

                    if (HealthCheckCodes.Contains(response.StatusCode))
                    {
                        log.Log(LogType.Health, LogLevel.Superficial, () => "Endpoint " + IpAddresses[i].Address + " passed its health check");

                        healthy = true;
                        IpAddresses[i].SetHealthy();
                    }
                    else
                    {
                        log.Log(LogType.Health, LogLevel.Superficial, () => "Endpoint " + IpAddresses[i].Address + " failed health check with status code " + response.StatusCode);

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

        private Response Send(Request request, ILog log)
        {
            try
            {
                var endpoint = new IPEndPoint(request.IpAddress, request.PortNumber);
                var key = request.Protocol + "://" + request.HostName + ":" + request.PortNumber + " " + request.IpAddress;

                ConnectionPool connectionPool;
                lock (_connectionPools)
                {
                    if (_connectionPools.TryGetValue(key, out connectionPool))
                    {
                        log?.Log(LogType.Pooling, LogLevel.VeryDetailed, () => "A connection pool exists for " + key);
                    }
                    else
                    {
                        log?.Log(LogType.Pooling, LogLevel.Superficial, () => "Creating new connection pool " + key);
                        connectionPool = new ConnectionPool(endpoint, request.HostName, request.Protocol, ConnectionTimeout, ResponseTimeout);
                        _connectionPools.Add(key, connectionPool);
                    }
                }

                var connection = connectionPool.GetConnection(log);
                try
                {
                    return connection.Send(request, log);
                }
                catch (Exception ex)
                {
                    log?.Log(LogType.TcpIp, LogLevel.Superficial, () => "Exception thrown sending request - " + ex.Message);
                    connection.Dispose();
                    throw;
                }
                finally
                {
                    connectionPool.ReuseConnection(log, connection);
                }
            }
            catch (Exception ex)
            {
                log?.Log(LogType.TcpIp, LogLevel.Superficial, () => "Returning 503 response because " + ex.Message);
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

        private class HealthCheckLog : ILog
        {
            private static volatile int _nextId;
            private readonly int _id;

            public HealthCheckLog()
            {
                _id = ++_nextId;
            }

            public void Dispose()
            {
            }

            public void Log(LogType type, LogLevel level, Func<string> messageFunc)
            {
                Trace.WriteLine($"[HEALTH] {_id,4} {type,-10} {messageFunc()}");
            }
        }
    }
}