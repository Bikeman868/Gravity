using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    public class ResponseConfiguration: NodeConfiguration
    {
        [JsonProperty("statusCode")]
        public ushort StatusCode { get; set; }

        [JsonProperty("reasonPhrase")]
        public string ReasonPhrase { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("contentFile")]
        public string ContentFile { get; set; }

        [JsonProperty("headers")]
        public ResponseHeaderConfiguration[] Headers { get; set; }

        public override void Sanitize()
        {
        }
    }
}