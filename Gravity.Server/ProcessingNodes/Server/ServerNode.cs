using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using Gravity.Server.Pipeline;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Server
{
    internal class ServerNodeException : ApplicationException
    {
        public ServerNode ServerNode;

        public ServerNodeException(ServerNode serverNode, string message, Exception innerException = null) :
            base("Server node '" + serverNode.Name + "' " + message, innerException)
        {
            ServerNode = serverNode;
        }
    }

    internal class ServerNode: INode
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public string DomainName { get; set; }
        public ushort? Port { get; set; }

        public TimeSpan ConnectionTimeout { get; set; }
        public TimeSpan ResponseTimeout { get; set; }
        public int ReadTimeoutMs { get; set; }
        public bool ReuseConnections { get; set; }

        public string HealthCheckMethod { get; set; }
        public string HealthCheckHost { get; set; }
        public ushort HealthCheckPort { get; set; }
        public PathString HealthCheckPath { get; set; }
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
        private readonly IBufferPool _bufferPool;
        private Thread _backgroundThread;
        private int _lastIpAddressIndex;

        public ServerNode(IBufferPool bufferPool)
        {
            _bufferPool = bufferPool;

            ConnectionTimeout = TimeSpan.FromSeconds(20);
            ResponseTimeout = TimeSpan.FromSeconds(10);
            ReadTimeoutMs = 200;
            ReuseConnections = true;
            DnsLookupInterval = TimeSpan.FromMinutes(5);
            RecalculateInterval = TimeSpan.FromSeconds(5);
            HealthCheckPort = 80;
            HealthCheckMethod = "GET";
            HealthCheckPath = new PathString("/");
            HealthCheckInterval = TimeSpan.FromMinutes(1);
            HealthCheckCodes = new[] { 200 };

            _connectionPools = new Dictionary<string, ConnectionPool>(StringComparer.OrdinalIgnoreCase);
        }

        public void Initialize()
        {
            _backgroundThread = new Thread(() =>
            {
                var nextDnsLookup = DateTime.UtcNow;
                var nextHealthCheck = DateTime.UtcNow.AddSeconds(2);
                var nextRecalculate = DateTime.UtcNow.AddSeconds(5);

                while (true)
                {
                    try
                    {
                        Thread.Sleep(100);
                        var timeNow = DateTime.UtcNow;

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

                        if (Disabled || string.IsNullOrEmpty(DomainName)) continue;

                        if (timeNow > nextDnsLookup)
                        {
                            using (var log = HealthCheckLog ? new HealthCheckLogger() : null)
                            {
                                nextDnsLookup = timeNow + LookupDomainName(log);
                            }
                        }

                        if (timeNow > nextHealthCheck)
                        {
                            nextHealthCheck = timeNow + HealthCheckInterval;

                            using (var log = HealthCheckLog ? new HealthCheckLogger() : null)
                            {
                                CheckHealth(log);
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

        Task INode.ProcessRequest(IRequestContext context)
        {
            var allIpAddresses = IpAddresses;

            if (allIpAddresses == null || allIpAddresses.Length == 0)
            {
                return Task.Run(() =>
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "No servers found with this host name";
                    context.Outgoing.SendHeaders(context);
                });
            }

            var ipAddresses = allIpAddresses.Where(i => i.Healthy == true).ToList();

            if (ipAddresses.Count == 0 || Healthy != true)
            {
                return Task.Run(() =>
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "No healthy servers";
                    context.Outgoing.SendHeaders(context);
                });
            }

            var ipAddressIndex = Interlocked.Increment(ref _lastIpAddressIndex) % ipAddresses.Count;
            var ipAddress = ipAddresses[ipAddressIndex];

            var port = (ushort)80;
            var scheme = context.Incoming.Scheme;

            if (Port.HasValue)
            {
                port = Port.Value;
                scheme = port == 443 ? Scheme.Https : Scheme.Http;
            }
            else if (scheme == Scheme.Https)
            {
                port = 443;
            }

            var serverRequestContext = (IRequestContext)new ServerRequestContext(context, ipAddress.Address, port, scheme);

            ipAddress.IncrementConnectionCount();
            var startTicks = ipAddress.TrafficAnalytics.BeginRequest();

            return Send(serverRequestContext)
                .ContinueWith(sendTask =>
                {
                    if (sendTask.IsFaulted)
                    {
                        context.Log?.Log(LogType.Exception, LogLevel.Important, () => $"Server node '{Name}' failed to send the request. {sendTask.Exception.Message}");
                        throw new ServerNodeException(this, "failed to send to server", sendTask.Exception);
                    }

                    if (sendTask.IsCanceled)
                    {
                        context.Log?.Log(LogType.Exception, LogLevel.Important, () => $"Server node '{Name}' timeout sending to server");
                        throw new ServerNodeException(this, "timeout sending to server");
                    }

                    ipAddress.TrafficAnalytics.EndRequest(startTicks);
                    ipAddress.DecrementConnectionCount();
                });
        }

        private TimeSpan LookupDomainName(ILog log)
        {
            if (IPAddress.TryParse(DomainName, out var ipAddress))
            {
                IpAddresses = new[]
                {
                    new ServerIpAddress
                    {
                        Address = ipAddress
                    }
                };
                return TimeSpan.FromMinutes(1);
            }
             
            try
            {
                log?.Log(LogType.Dns, LogLevel.Detailed, () => $"Looking up IP address for {DomainName}");

                var hostEntry = Dns.GetHostEntry(DomainName);

                if (hostEntry.AddressList == null || hostEntry.AddressList.Length == 0)
                {
                    log?.Log(LogType.Dns, LogLevel.Important, () => "DNS returned no IP addresses");

                    UnhealthyReason = "DNS returned no IP addresses for " + DomainName;
                    Healthy = false;
                    return TimeSpan.FromSeconds(10);
                }

                log?.Log(LogType.Dns, LogLevel.Detailed, () => "DNS returned " + string.Join(", ", hostEntry.AddressList.Select(a => a.ToString())));

                var newIpAddresses = hostEntry.AddressList
                    .Select(a => new ServerIpAddress { Address = a })
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
                return DnsLookupInterval;
            }
            catch (Exception ex)
            {
                log?.Log(LogType.Exception, LogLevel.Important, () => "Exception in DNS lookup of " + DomainName + ". " + ex.Message);

                UnhealthyReason = ex.Message + " " + DomainName;
                Healthy = false;
                return TimeSpan.FromSeconds(10);
            }
        }

        private void CheckHealth(ILog log)
        {
            var host = HealthCheckHost ?? DomainName;

            var healthy = false;
            var tasks = new List<Task>();

            foreach (var ipAddress in IpAddresses)
            {
                try
                {
                    log?.Log(LogType.Health, LogLevel.Important, () => $"Checking health of endpoint {ipAddress.Address}");

                    var requestContext = (IRequestContext)new ServerRequestContext(
                        log,
                        ipAddress.Address, 
                        HealthCheckPort,
                        HealthCheckPort == 443 ? Scheme.Https : Scheme.Http,
                        HealthCheckHost ?? DomainName,
                        HealthCheckMethod,
                        HealthCheckPath,
                        new QueryString());

                    tasks.Add(
                        Send(requestContext)
                            .ContinueWith(sendTask =>
                            {
                                if (sendTask.IsFaulted)
                                {
                                    log?.Log(LogType.Health, LogLevel.Important, () => $"Endpoint {ipAddress.Address} failed health check, send exception {sendTask.Exception?.Message}");
                                    ipAddress.SetUnhealthy(sendTask.Exception.Message);
                                }
                                else if (sendTask.IsCanceled)
                                {
                                    log?.Log(LogType.Health, LogLevel.Important, () => $"Endpoint {ipAddress.Address} failed health check, send task timeout");
                                    ipAddress.SetUnhealthy("Send task timeout");
                                }
                                else
                                {
                                    if (HealthCheckCodes.Contains(requestContext.Outgoing.StatusCode))
                                    {
                                        log?.Log(LogType.Health, LogLevel.Important, () => $"Endpoint {ipAddress.Address} passed its health check");

                                        healthy = true;
                                        ipAddress.SetHealthy();
                                    }
                                    else
                                    {
                                        log?.Log(LogType.Health, LogLevel.Important, () => $"Endpoint {ipAddress.Address} failed health check with status code {requestContext.Outgoing.StatusCode} {requestContext.Outgoing.ReasonPhrase}");
                                        ipAddress.SetUnhealthy(requestContext.Outgoing.StatusCode + " " + requestContext.Outgoing.ReasonPhrase);
                                    }
                                }
                            }));
                }
                catch (Exception ex)
                {
                    ipAddress.SetUnhealthy(ex.Message);
                }
            }

            Task.WaitAll(tasks.ToArray());

            if (healthy)
                Healthy = true;
            else
            {
                UnhealthyReason = "No healthy IP addresses";
                Healthy = false;
            }
        }

        private Task Send(IRequestContext context)
        {
            var endpoint = new IPEndPoint(context.Incoming.DestinationAddress, context.Incoming.DestinationPort);
            var key = context.Incoming.Scheme + "://" + context.Incoming.DomainName + ":" + context.Incoming.DestinationPort + " " + context.Incoming.DestinationAddress;
            key = key.ToLower();

            ConnectionPool connectionPool;
            lock (_connectionPools)
            {
                if (_connectionPools.TryGetValue(key, out connectionPool))
                {
                    context.Log?.Log(LogType.Pooling, LogLevel.VeryDetailed, () => "A connection pool exists for " + key);
                }
                else
                {
                    context.Log?.Log(LogType.Pooling, LogLevel.Important, () => "Creating new connection pool " + key);
                    connectionPool = new ConnectionPool(_bufferPool, endpoint, context.Incoming.DomainName, context.Incoming.Scheme, ConnectionTimeout);
                    _connectionPools.Add(key, connectionPool);
                }
            }

            return connectionPool.GetConnection(context.Log, ResponseTimeout, ReadTimeoutMs)
                .ContinueWith(connectionTask =>
                {
                    if (connectionTask.IsFaulted)
                    {
                        context.Log?.Log(LogType.TcpIp, LogLevel.Important, () => "Connection task faulted with " + connectionTask.Exception.Message);
                        throw new ServerNodeException(this, "Connection task timed out", connectionTask.Exception);
                    }

                    if (connectionTask.IsCanceled)
                    {
                        context.Log?.Log(LogType.TcpIp, LogLevel.Important, () => "Connection task timed out");
                        throw new ServerNodeException(this, "Connection task timed out");
                    }

                    var connection = connectionTask.Result;

                    try
                    {
                        connection.Send(context, ResponseTimeout, ReadTimeoutMs).Wait();
                        connectionPool.ReuseConnection(context.Log, connection);
                    }
                    catch (Exception ex)
                    {
                        context.Log?.Log(LogType.TcpIp, LogLevel.Important, () => "Returning 503 response because " + ex.Message);

                        context.Outgoing.StatusCode = 503;
                        context.Outgoing.ReasonPhrase = "Exception forwarding request to real server";
                        context.Outgoing.SendHeaders(context);
                    }
                });
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