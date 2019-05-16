using Gravity.Server.DataStructures;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class ListenerEndpointConfiguration
    {
        [JsonProperty("disabled")]
        public bool Disabled { get; set; }

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