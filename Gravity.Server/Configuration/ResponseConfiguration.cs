using Gravity.Server.ProcessingNodes;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class ResponseConfiguration: NodeConfiguration
    {
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }

        [JsonProperty("reasonPhrase")]
        public string ReasonPhrase { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("headers")]
        public ResponseHeaderConfiguration[] Headers { get; set; }
    }
}