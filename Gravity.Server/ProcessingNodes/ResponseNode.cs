using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes
{
    public class ResponseNode: INode
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public string Content { get; set; }
        public string[] HeaderNames { get; set; }
        public string[] HeaderValues { get; set; }

        void INode.Bind(INodeGraph nodeGraph)
        {
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            context.Response.StatusCode = StatusCode;
            context.Response.ReasonPhrase = ReasonPhrase;

            if (HeaderNames != null)
            {
                for (var i = 0; i < HeaderNames.Length; i++) 
                    context.Response.Headers[HeaderNames[i]] = HeaderValues[i];
            }

            return context.Response.WriteAsync(Content);
        }
    }
}