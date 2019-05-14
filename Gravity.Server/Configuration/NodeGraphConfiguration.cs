using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class NodeGraphConfiguration
    {
        [JsonProperty("servers")]
        public ServerNode[] ServerNodes { get; set; }

        [JsonProperty("roundRobin")]
        public RoundRobinNode[] RoundRobinNodes { get; set; }

        public NodeGraphConfiguration Sanitize()
        {
            if (ServerNodes == null)
            {
                RoundRobinNodes = new[]
                {
                    new RoundRobinNode{Name = "A", Outputs = new []{ "B", "C", "D" }}
                };

                ServerNodes = new[]
                {
                    new ServerNode { Name = "B" },
                    new ServerNode { Name = "C" },
                    new ServerNode { Name = "D" },
                };
            }
            return this;
        }
    }
}