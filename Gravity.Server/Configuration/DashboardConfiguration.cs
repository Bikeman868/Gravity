using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    public class DashboardConfiguration
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("listeners")]
        public NodeConfiguration[] Listeners { get; set; }

        [JsonProperty("nodes")]
        public NodeConfiguration[] Nodes { get; set; }

        [JsonProperty("trafficIndicator")]
        public TrafficIndicatorConfiguration TrafficIndicator { get; set; }

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
                            Title = null,
                            X = x,
                            Y = x,
                            Width = width,
                            Height = height
                        });

                    y += 80;
                };

                Nodes = nodes.ToArray();
            }
            else
            {
                foreach (var node in Nodes) node.Sanitize();
            }

            if (TrafficIndicator == null)
                TrafficIndicator = new TrafficIndicatorConfiguration();
            TrafficIndicator.Sanitize();

            if (Listeners == null)
            {
                var listeners = new List<NodeConfiguration>();

                var x = 0;
                var y = 50;
                var width = 300;
                var height = 50;

                for (var c = 'A'; c < 'Z'; c++)
                {
                    listeners.Add(
                        new NodeConfiguration
                        {
                            NodeName = new string(new[] { c }),
                            Title = null,
                            X = x,
                            Y = x,
                            Width = width,
                            Height = height
                        });

                    y += 80;
                };

                Listeners = listeners.ToArray();
            }
            else
            {
                foreach (var listener in Listeners) listener.Sanitize();
            }

            return this;
        }

        public class NodeConfiguration
        {
            [JsonProperty("name")]
            public string NodeName { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("x")]
            public int X { get; set; }

            [JsonProperty("y")]
            public int Y { get; set; }

            [JsonProperty("width")]
            public int Width { get; set; }

            [JsonProperty("height")]
            public int Height { get; set; }

            public void Sanitize()
            {
                if (X < 0) X = 0;
                if (X > 10000) X = 10000;

                if (Y < 0) Y = 0;
                if (Y > 10000) Y = 10000;

                if (Width < 50) Width = 50;
                if (Width > 1000) Width = 1000;

                if (Height < 50) Height = 50;
                if (Height > 1000) Height = 1000;
            }
        }
    }
}