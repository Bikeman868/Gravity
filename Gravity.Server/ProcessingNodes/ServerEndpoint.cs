using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes
{
    public class ServerEndpoint: INode
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }

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