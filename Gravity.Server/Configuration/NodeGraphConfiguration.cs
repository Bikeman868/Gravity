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
        [JsonProperty("cors")]
        public CorsConfiguration[] CorsNodes { get; set; }

        [JsonProperty("internalEndpoints")]
        public InternalEndpointConfiguration[] InternalNodes { get; set; }

        [JsonProperty("leastConnections")]
        public LeastConnectionsConfiguration[] LeastConnectionsNodes { get; set; }

        [JsonProperty("roundRobin")]
        public RoundRobinConfiguration[] RoundRobinNodes { get; set; }

        [JsonProperty("responses")]
        public ResponseConfiguration[] ResponseNodes { get; set; }

        [JsonProperty("routers")]
        public RouterConfiguration[] RouterNodes { get; set; }

        [JsonProperty("servers")]
        public ServerConfiguration[] ServerNodes { get; set; }

        [JsonProperty("stickySessions")]
        public StickySessionConfiguration[] StickySessionNodes { get; set; }

        [JsonProperty("transforms")]
        public TransformConfiguration[] TransformNodes { get; set; }

        public NodeGraphConfiguration Sanitize()
        {
            if (ServerNodes == null)
            {
                // Define the default node graph to use when there is no configuration

                InternalNodes = new[]
                {
                    new InternalEndpointConfiguration {Name = "A"}
                };
            }
            else
            {
                // Check for circular graphs
            }
            return this;
        }
    }
}