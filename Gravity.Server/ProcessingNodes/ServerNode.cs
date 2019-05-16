using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes
{
    public class ServerNode: INode
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public bool Healthy { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }
        public TimeSpan RequestTimeout { get; set; }
        public string HealthCheckMethod { get; set; }
        public string HealthCheckHost { get; set; }
        public int HealthCheckPort { get; set; }
        public string HealthCheckPath { get; set; }

        void INode.Bind(INodeGraph nodeGraph)
        {
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            context.Response.StatusCode = 200;
            context.Response.ReasonPhrase = "OK";
            return context.Response.WriteAsync(Name);
        }
    }
}