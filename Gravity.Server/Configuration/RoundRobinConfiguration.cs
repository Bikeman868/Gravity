using Gravity.Server.ProcessingNodes;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class RoundRobinConfiguration: NodeConfiguration
    {
        [JsonProperty("outputs")]
        public string[] Outputs { get; set; }
    }
}