using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class LeastConnectionsConfiguration: NodeConfiguration
    {
        [JsonProperty("outputs")]
        public string[] Outputs { get; set; }
    }
}