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
        public int ReadTimeoutMs { get; set; }
        public bool ReuseConnections { get; set; }

        public string HealthCheckMethod { get; set; }
        public string HealthCheckHost { get; set; }
        public int HealthCheckPort { get; set; }
        public string HealthCheckPath { get; set; }
        public int[] HealthCheckCodes { get; set; }
        public bool HealthCheckLog { get; set; }
        public TimeSpan HealthCheckInterval { get; set; }

        public TimeSpan DnsLookupInterval { get; set; }
        public TimeSpan RecalculateInterval { get; set; }

        public bool? Healthy { get; private set; }
        public string UnhealthyReason { get; private set; }
        public bool Offline { get; private set; }

        public ServerIpAddress[] IpAddresses;
        private readonly Dictionary<string, ConnectionPool> _connectionPools;

        private Thread _backgroundThread;
        private DateTime _nextDnsLookup;
        private int _lastIpAddressIndex;

        public ServerNode()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(20);
            ResponseTimeout = TimeSpan.FromSeconds(10);
            ReadTimeoutMs = 200;
            ReuseConnections = true;
            DnsLookupInterval = TimeSpan.FromMinutes(5);
            RecalculateInterval = TimeSpan.FromSeconds(5);
            HealthCheckPort = 80;
            HealthCheckMethod = "GET";
            HealthCheckPath = "/";
            HealthCheckInterval = TimeSpan.FromMinutes(1);
            HealthCheckCodes = new[] { 200 };

            _connectionPools = new Dictionary<string, ConnectionPool>();
        }

        public void Initialize()
        {
            _backgroundThread = new Thread(() =>
            {
                var nextHealthCheck = DateTime.UtcNow;
                var nextRecalculate = DateTime.UtcNow.AddSeconds(5);

                while (true)
                {
                    try
                    {
                        Thread.Sleep(100);
                        var timeNow = DateTime.UtcNow;

                        if (!Disabled && timeNow > nextHealthCheck && !string.IsNullOrEmpty(Host))
                        {
                            nextHealthCheck = timeNow + HealthCheckInterval;

                            using (var log = HealthCheckLog ? new HealthCheckLogger() : null)
                            {
                                CheckHealth(log);
                            }
                        }

                        if (timeNow > nextRecalculate)
                        {
                            nextRecalculate = timeNow + RecalculateInterval;
                            var ipAddresses = IpAddresses;
                            if (ipAddresses != null)
                            {
                                for (var i = 0; i < ipAddresses.Length; i++)
                                    ipAddresses[i].TrafficAnalytics.Recalculate();
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
                Name = "Server node " + Name,
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
        }

        public void Dispose()
        {
            _backgroundThread?.Abort();
            _backgroundThread?.Join(TimeSpan.FromSeconds(60));
            _backgroundThread = null;

            lock (_connectionPools)
            {
                foreach (var endpoint in _connectionPools.Values)
                    endpoint.Dispose();
                _connectionPools.Clear();
            }
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
            _backgroundThread.Start();
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
                var retry = 0;
                while (true)
                {
                    try
                    {
                        response = Send(request, log);
                        break;
                    }
                    catch
                    {
                        if (++retry > 2)
                        {
                            log?.Log(LogType.Exception, LogLevel.Important, () => $"Request to server node {Name} failed {retry} times and will not be retried");
                            throw;
                        }
                        else
                        {
                            log?.Log(LogType.Exception, LogLevel.Standard, () => $"Request to server node {Name} failed and will be retried");
                        }
                    }
                }
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
                        log?.Log(LogType.Health, LogLevel.Detailed, () => $"Looking up IP address for {Host}");

                        var hostEntry = Dns.GetHostEntry(Host);

                        if (hostEntry.AddressList == null || hostEntry.AddressList.Length == 0)
                        {
                            log?.Log(LogType.Health, LogLevel.Important, () => "DNS returned no IP addresses");

                            UnhealthyReason = "DNS returned no IP addresses for " + Host;
                            Healthy = false;
                            _nextDnsLookup = DateTime.UtcNow.AddSeconds(10);
                            return;
                        }

                        log?.Log(LogType.Health, LogLevel.Detailed, () => "DNS returned " + string.Join(", ", hostEntry.AddressList.Select(a => a.ToString())));

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
                        log?.Log(LogType.Exception, LogLevel.Important, () => "Exception in DNS lookup of " + Host + ". " + ex.Message);

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
                    log?.Log(LogType.Health, LogLevel.Important, () => "Checking health of endpoint " + IpAddresses[i].Address);

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
                        log?.Log(LogType.Health, LogLevel.Important, () => "Endpoint " + IpAddresses[i].Address + " passed its health check");

                        healthy = true;
                        IpAddresses[i].SetHealthy();
                    }
                    else
                    {
                        log?.Log(LogType.Health, LogLevel.Important, () => "Endpoint " + IpAddresses[i].Address + " failed health check with status code " + response.StatusCode + " " + response.ReasonPhrase);
                        IpAddresses[i].SetUnhealthy(response.StatusCode + " " + response.ReasonPhrase);
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
                        log?.Log(LogType.Pooling, LogLevel.Important, () => "Creating new connection pool " + key);
                        connectionPool = new ConnectionPool(endpoint, request.HostName, request.Protocol, ConnectionTimeout);
                        _connectionPools.Add(key, connectionPool);
                    }
                }

                var connection = connectionPool.GetConnection(log, ResponseTimeout, ReadTimeoutMs);
                try
                {
                    return connection.Send(request, log);
                }
                catch (Exception ex)
                {
                    log?.Log(LogType.TcpIp, LogLevel.Important, () => "Exception thrown sending request - " + ex.Message);
                    connection.Dispose();
                    throw;
                }
                finally
                {
                    if (ReuseConnections)
                        connectionPool.ReuseConnection(log, connection);
                    else
                        connection.Dispose();
                }
            }
            catch (Exception ex)
            {
                log?.Log(LogType.TcpIp, LogLevel.Important, () => "Returning 503 response because " + ex.Message);
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

        private class HealthCheckLogger : ILog
        {
            private static volatile int _nextId;
            private readonly int _id;
            private readonly DateTime _startTime = DateTime.UtcNow;

            public HealthCheckLogger()
            {
                _id = ++_nextId;
            }

            public void Dispose()
            {
            }

            public void Log(LogType type, LogLevel level, Func<string> messageFunc)
            {
                var elapsed = (int)(DateTime.UtcNow - _startTime).TotalMilliseconds;
                Trace.WriteLine($"[HEALTH-CHECK] {_id,6} {elapsed,6}ms {type,-10} {messageFunc()}");
            }
        }
    }
}