using Gravity.Server.ProcessingNodes;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class TransformConfiguration: NodeConfiguration
    {
        [JsonProperty("script")]
        public string Script { get; set; }
    }
}