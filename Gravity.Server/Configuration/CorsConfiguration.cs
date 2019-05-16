using Gravity.Server.ProcessingNodes;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class CorsConfiguration: NodeConfiguration
    {
        /// <summary>
        /// The node to send thr request to after CORS logic
        /// </summary>
        [JsonProperty("output")]
        public string OutputNode { get; set; }
    }
}