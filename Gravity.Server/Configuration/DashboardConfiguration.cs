using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class DashboardConfiguration
    {
        [JsonProperty("nodes")]
        public NodeConfiguration[] Nodes { get; set; }

        public DashboardConfiguration Sanitize()
        {
            if (Nodes == null)
            {
                var nodes = new List<NodeConfiguration>();

                var x = 100;
                var y = 100;
                var width = 300;
                var height = 50;

                for (var c = 'A'; c < 'Z'; c++)
                {
                    nodes.Add(
                        new NodeConfiguration
                        {
                            NodeName = new string(new []{c}),
                            X = x,
                            Y = x,
                            Width = width,
                            Height = height
                        });

                    y += 80;
                };

                Nodes = nodes.ToArray();
            }

            return this;
        }

        public class NodeConfiguration
        {
            [JsonProperty("name")]
            public string NodeName { get; set; }

            [JsonProperty("x")]
            public int X { get; set; }

            [JsonProperty("y")]
            public int Y { get; set; }

            [JsonProperty("width")]
            public int Width { get; set; }

            [JsonProperty("height")]
            public int Height { get; set; }
        }
    }
}