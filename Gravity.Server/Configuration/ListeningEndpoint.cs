using Gravity.Server.DataStructures;
using Gravity.Server.ProcessingNodes;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class ListeningEndpoint
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("ipAddress")]
        public string IpAddress { get; set; }

        [JsonProperty("port")]
        public int PortNumber { get; set; }

        [JsonProperty("node")]
        public string NodeName { get; set; }

        [JsonIgnore]
        public NodeOutput ProcessingNode { get; set; }
    }
}