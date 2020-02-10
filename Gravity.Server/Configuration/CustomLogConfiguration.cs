using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Gravity.Server.Interfaces;
using Gravity.Server.Utility;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    public class CustomLogConfiguration: NodeConfiguration
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
        /// Note that it does not make sense to have both an include and an exclude list
        /// </summary>
        [JsonProperty("statusCodes")]
        public ushort[] IncludeStatusCodes { get; set; }

        /// <summary>
        /// Do not log these status codes, blank for all status codes
        /// Note that it does not make sense to have both an include and an exclude list
        /// </summary>
        [JsonProperty("excludeStatusCodes")]
        public ushort[] ExcludeStatusCodes { get; set; }

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

        public override void Sanitize()
        {
            if (Methods != null)
            {
                var allowedMethods = new[] { "POST", "PUT", "DELETE", "GET", "HEAD", "OPTIONS" };
                Methods = Methods.Select(m => m.ToUpper()).Where(m => allowedMethods.Contains(m)).ToArray();
            }

            if (string.IsNullOrEmpty(Directory))
                Directory = "C:\\Logs\\Custom\\";
            else
                if (!Directory.EndsWith("\\")) Directory = Directory + "\\";

            if (FileNamePrefix == null) FileNamePrefix = Name + "_";

            if (MaximumLogFileAge == default(TimeSpan)) MaximumLogFileAge = TimeSpan.FromHours(6);
            if (MaximumLogFileAge < TimeSpan.FromMinutes(10)) MaximumLogFileAge = TimeSpan.FromMinutes(10);
            if (MaximumLogFileAge > TimeSpan.FromDays(90)) MaximumLogFileAge = TimeSpan.FromDays(90);

            if (MaximumLogFileSize == default(long)) MaximumLogFileSize = 100000;
            if (MaximumLogFileSize < 1000) MaximumLogFileSize = 1000;
            if (MaximumLogFileSize > 100000000) MaximumLogFileSize = 100000000;

            ContentType = "text/plain"; // Currently this is the only supported option
        }
    }
}