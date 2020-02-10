using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    public class ChangeLogFilterConfiguration: NodeConfiguration
    {
        /// <summary>
        /// The node to send thr request to next
        /// </summary>
        [JsonProperty("output")]
        public string OutputNode { get; set; }

        /// <summary>
        /// The new logging level to apply
        /// </summary>
        [JsonProperty("maxLogLevel")]
        public LogLevel MaximumLogLevel { get; set; }

        /// <summary>
        /// The types of message to log for this request
        /// </summary>
        [JsonProperty("logTypes")]
        public LogType[] LogTypes { get; set; }
    }
}