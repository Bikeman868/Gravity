using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes
{
    internal class ServerNode: INode, IDisposable
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public bool Healthy { get; private set; }
        public string UnhealthyReason { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }
        public TimeSpan RequestTimeout { get; set; }
        public string HealthCheckMethod { get; set; }
        public string HealthCheckHost { get; set; }
        public int HealthCheckPort { get; set; }
        public string HealthCheckPath { get; set; }

        private readonly Thread _heathCheckThread;

        public ServerNode()
        {
            Port = 80;
            ConnectionTimeout = TimeSpan.FromSeconds(5);
            RequestTimeout = TimeSpan.FromMinutes(1);

            Healthy = true;
            HealthCheckPort = 80;
            HealthCheckMethod = "GET";
            HealthCheckPath = "/";

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
            context.Response.StatusCode = 200;
            context.Response.ReasonPhrase = "OK";
            return context.Response.WriteAsync(Name);
        }

        private void CheckHealth()
        {
            IPAddress ipAddress;

            if (!IPAddress.TryParse(Host, out ipAddress))
            {
                try
                {
                    var hostEntry = Dns.GetHostEntry(Host);
                    if (hostEntry.AddressList == null || hostEntry.AddressList.Length == 0)
                    {
                        UnhealthyReason = "DNS returned no IP addresses for " + Host;
                        Healthy = false;
                        return;
                    }
                    ipAddress = hostEntry.AddressList[0];
                }
                catch (Exception ex)
                {
                    UnhealthyReason = ex.Message + " " + Host;
                    Healthy = false;
                    return;
                }
            }

            var host = HealthCheckHost ?? Host;
            if (HealthCheckPort != 80) host += ":" + HealthCheckPort;

            var request = new Request
            {
                IpAddress = ipAddress.ToString(),
                PortNumber = HealthCheckPort,
                Method = HealthCheckMethod,
                PathAndQuery = HealthCheckPath,
                Headers = new[]
                {
                    new Tuple<string, string>( "Host", host)
                }
            };

            var response = Send(request);

            if (response.StatusCode == 200)
            {
                Healthy = true;
            }
            else
            {
                UnhealthyReason = "Status code " + response.StatusCode;
                Healthy = false;
            }
        }

        private Response Send(Request request)
        {
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