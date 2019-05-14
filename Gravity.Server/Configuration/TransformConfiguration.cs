﻿using Gravity.Server.ProcessingNodes;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class TransformConfiguration: NodeConfiguration
    {
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
        public string RequestScript { get; set; }

        /// <summary>
        /// Defines transfoormation logic to apply to response from the real
        /// servers before returning it to the original caller
        /// </summary>
        [JsonProperty("responseScript")]
        public string ResponseScript { get; set; }
    }
}