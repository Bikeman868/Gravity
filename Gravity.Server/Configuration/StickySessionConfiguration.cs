using Gravity.Server.ProcessingNodes;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class StickySessionConfiguration: NodeConfiguration
    {
        [JsonProperty("outputs")]
        public string[] Outputs { get; set; }

        [JsonProperty("cookie")]
        public string SesionCookie { get; set; }
    }
}