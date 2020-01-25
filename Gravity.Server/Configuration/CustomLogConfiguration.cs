using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class CustomLogConfiguration: NodeConfiguration
    {
        /// <summary>
        /// The node to send thr request to next
        /// </summary>
        [JsonProperty("output")]
        public string OutputNode { get; set; }

        /// <summary>
        /// The only log these methods, blank for all methods
        /// </summary>
        [JsonProperty("methods")]
        public string[] Methods { get; set; }

        /// <summary>
        /// Only log these status codes, blank for all status codes
        /// </summary>
        [JsonProperty("statusCodes")]
        public ushort[] StatusCodes { get; set; }

        /// <summary>
        /// Only folder to write log files to
        /// </summary>
        [JsonProperty("directory")]
        public string Directory { get; set; }

        /// <summary>
        /// Prefix for file names
        /// </summary>
        [JsonProperty("fileNamePrefix")]
        public string FileNamePrefix { get; set; }

        /// <summary>
        /// How long to keep the log files
        /// </summary>
        [JsonProperty("maximumLogFileAge")]
        public TimeSpan MaximumLogFileAge { get; set; }

        /// <summary>
        /// Maximum file size before starting a new log file
        /// </summary>
        [JsonProperty("maximumLogFileSize")]
        public long MaximumLogFileSize { get; set; }

        /// <summary>
        /// True to write multiple lines per request
        /// </summary>
        [JsonProperty("detailed")]
        public bool Detailed { get; set; }

        /// <summary>
        /// The mime type of the log files to create
        /// </summary>
        [JsonProperty("contentType")]
        public string ContentType { get; set; }
    }
}