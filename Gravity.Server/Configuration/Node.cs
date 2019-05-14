using Gravity.Server.ProcessingNodes;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class Node
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}