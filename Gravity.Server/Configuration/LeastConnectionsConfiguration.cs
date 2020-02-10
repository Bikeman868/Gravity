using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    public class LeastConnectionsConfiguration: NodeConfiguration
    {
        [JsonProperty("outputs")]
        public string[] Outputs { get; set; }
    }
}