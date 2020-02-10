using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    public class NodeGraphConfiguration
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

        [JsonProperty("changeLogFilters")]
        public ChangeLogFilterConfiguration[] ChangeLogFilterNodes { get; set; }

        [JsonProperty("customLogs")]
        public CustomLogConfiguration[] CustomLogNodes { get; set; }

        public NodeGraphConfiguration Sanitize()
        {
            if (
                (InternalNodes == null || InternalNodes.Length == 0) &&
                (ResponseNodes == null || ResponseNodes.Length == 0) &&
                (ServerNodes == null || ServerNodes.Length == 0) &&
                (CustomLogNodes == null || CustomLogNodes.Length == 0))
            {
                // If there is no valid configuration then send all requests to the UI
                InternalNodes = new[] { new InternalEndpointConfiguration { Name = "A" } };
            }

            if (CorsNodes != null) foreach (var node in CorsNodes) node.Sanitize();
            if (InternalNodes != null) foreach (var node in InternalNodes) node.Sanitize();
            if (LeastConnectionsNodes != null) foreach (var node in LeastConnectionsNodes) node.Sanitize();
            if (RoundRobinNodes != null) foreach (var node in RoundRobinNodes) node.Sanitize();
            if (ResponseNodes != null) foreach (var node in ResponseNodes) node.Sanitize();
            if (RouterNodes != null) foreach (var node in RouterNodes) node.Sanitize();
            if (ServerNodes != null) foreach (var node in ServerNodes) node.Sanitize();
            if (StickySessionNodes != null) foreach (var node in StickySessionNodes) node.Sanitize();
            if (TransformNodes != null) foreach (var node in TransformNodes) node.Sanitize();
            if (ChangeLogFilterNodes != null) foreach (var node in ChangeLogFilterNodes) node.Sanitize();
            if (CustomLogNodes != null) foreach (var node in CustomLogNodes) node.Sanitize();

            // TODO: Check for circular graphs

            return this;
        }
    }
}