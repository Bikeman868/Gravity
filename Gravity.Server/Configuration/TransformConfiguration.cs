using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    public class TransformConfiguration: NodeConfiguration
    {
        /// <summary>
        /// The node to send thr request to after transformation
        /// </summary>
        [JsonProperty("output")]
        public string OutputNode { get; set; }

        /// <summary>
        /// A description of what this transform does
        /// </summary>
        [JsonProperty("description")]
        public string[] Description { get; set; }

        /// <summary>
        /// The language used to write the scripts ion this node
        /// </summary>
        [JsonProperty("scriptLanguage")]
        public ScriptLanguage ScriptLanguage { get; set; }

        /// <summary>
        /// Defines transformation logic to apply to incomming request before
        /// it is forwarded to the real servers
        /// </summary>
        [JsonProperty("requestScript")]
        public string[] RequestScript { get; set; }

        /// <summary>
        /// Defines transfoormation logic to apply to response from the real
        /// servers before returning it to the original caller
        /// </summary>
        [JsonProperty("responseScript")]
        public string[] ResponseScript { get; set; }

        /// <summary>
        /// Defines request transformation logic contained in a file
        /// </summary>
        [JsonProperty("requestScriptFile")]
        public string RequestScriptFile { get; set; }

        /// <summary>
        /// Defines response transformation logic contained in a file
        /// </summary>
        [JsonProperty("responseScriptFile")]
        public string ResponseScriptFile { get; set; }

        public override void Sanitize()
        {
        }
    }
}