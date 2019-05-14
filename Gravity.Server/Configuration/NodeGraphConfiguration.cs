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
        [JsonProperty("pages")]
        public InternalPageConfiguration[] InternalPageNodes { get; set; }

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

                RouterNodes = new[]
                {
                    new RouterConfiguration
                    {
                        Name = "A",
                        Outputs = new[]
                        {
                            new RouterOutputConfiguration
                            {
                                NodeName = "B",
                                RuleLogic = RuleLogic.All,
                                Rules = new[]
                                {
                                    new RouterRuleConfiguration {Condition = "Path[1] = 'ui'"}
                                }
                            },
                            new RouterOutputConfiguration
                            {
                                NodeName = "C"
                            }
                        }
                    }
                };

                InternalPageNodes = new[]
                {
                    new InternalPageConfiguration {Name = "B"}
                };

                RoundRobinNodes = new[]
                {
                    new RoundRobinConfiguration {Name = "C", Outputs = new[] {"D", "E", "F"}}
                };

                ServerNodes = new[]
                {
                    new ServerConfiguration {Name = "D"},
                    new ServerConfiguration {Name = "E"},
                    new ServerConfiguration {Name = "F"},
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