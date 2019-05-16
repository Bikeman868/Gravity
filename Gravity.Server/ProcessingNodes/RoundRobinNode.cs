﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gravity.Server.DataStructures;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes
{
    public class RoundRobinNode: INode
    {
        public string Name { get; set; }
        public string[] Outputs { get; set; }
        public bool Disabled { get; set; }

        private int _next;
        private NodeOutput[] _outputs;

        void INode.Bind(INodeGraph nodeGraph)
        {
            _outputs = Outputs.Select(name => new NodeOutput
            {
                Name = name,
                Node = nodeGraph.NodeByName(name),
            }).ToArray();
        }

        Task INode.ProcessRequest(IOwinContext context)
        {
            if (Disabled)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "Balancer " + Name + " is disabled";
                return context.Response.WriteAsync(string.Empty);
            }

            var outputs = _outputs;

            if (outputs == null || outputs.Length == 0)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "Balancer " + Name + " has no outputs";
                return context.Response.WriteAsync(string.Empty);
            }

            var enabledOutputs = outputs.Where(o => !o.Disabled && o.Node != null).ToList();

            if (enabledOutputs.Count == 0)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "All balancer " + Name + " outputs are disabled";
                return context.Response.WriteAsync(string.Empty);
            }

            var index = Interlocked.Increment(ref _next) % enabledOutputs.Count;
            var output = enabledOutputs[index];

            output.IncrementRequestCount();
            return output.Node.ProcessRequest(context);
        }
    }
}