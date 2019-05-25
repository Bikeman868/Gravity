using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class DashboardConfiguration
    {
        [JsonProperty("listeners")]
        public ListenersConfiguration Listeners { get; set; }

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
                            X = x,
                            Y = x,
                            Width = width,
                            Height = height
                        });

                    y += 80;
                };

                Nodes = nodes.ToArray();
            }

            if (TrafficIndicator == null)
                TrafficIndicator = new TrafficIndicatorConfiguration();
            TrafficIndicator.Sanitize();

            if (Listeners == null)
            {
                Listeners = new ListenersConfiguration
                {
                    X = 0,
                    Y = 50,
                    XSpacing = 0,
                    YSpacing = 150
                };
            }
            else
            {
                if (Listeners.X < 0) Listeners.X = 0;
                if (Listeners.X > 10000) Listeners.X = 10000;
                if (Listeners.Y < 0) Listeners.Y = 0;
                if (Listeners.Y > 10000) Listeners.Y = 10000;
            }

            return this;
        }

        public class ListenersConfiguration
        {
            [JsonProperty("x")]
            public int X { get; set; }

            [JsonProperty("y")]
            public int Y { get; set; }

            [JsonProperty("xSpacing")]
            public int XSpacing { get; set; }

            [JsonProperty("ySpacing")]
            public int YSpacing { get; set; }
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