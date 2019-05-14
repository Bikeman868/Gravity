using Gravity.Server.ProcessingNodes;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class RoundRobinNode: Node
    {
        [JsonProperty("outputs")]
        public string[] Outputs { get; set; }
    }
}