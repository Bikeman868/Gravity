using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    public class RoundRobinConfiguration: NodeConfiguration
    {
        [JsonProperty("outputs")]
        public string[] Outputs { get; set; }
    }
}