using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    public class CorsConfiguration: NodeConfiguration
    {
        /// <summary>
        /// The node to send thr request to after CORS logic
        /// </summary>
        [JsonProperty("output")]
        public string OutputNode { get; set; }

        /// <summary>
        /// The main origin of this website
        /// </summary>
        [JsonProperty("websiteOrigin")]
        public string WebsiteOrigin { get; set; }

        /// <summary>
        /// A regular expression that matches allowed origins
        /// </summary>
        [JsonProperty("allowedOrigins")]
        public string AllowedOrigins { get; set; }

        /// <summary>
        /// Comma separated list of request headers that are allowed
        /// </summary>
        [JsonProperty("allowedHeaders")]
        public string AllowedHeaders { get; set; }

        /// <summary>
        /// Comma separated list of request methods that are allowed
        /// </summary>
        [JsonProperty("allowedMethods")]
        public string AllowedMethods { get; set; }

        /// <summary>
        /// True if JavaScript is allowed to supply credentials
        /// </summary>
        [JsonProperty("allowCredentials")]
        public bool AllowCredentials { get; set; }

        /// <summary>
        /// Comma separated list of headers that are exposed to JavaScript
        /// </summary>
        [JsonProperty("exposedHeaders")]
        public string ExposedHeaders { get; set; }

        public CorsConfiguration()
        {
            WebsiteOrigin = "https://mycompany.com";
            AllowedOrigins = @"https?://(.+\.)?mycompany\.com";
            AllowedHeaders = "Accept,Content-Type,Location";
            AllowedMethods = "GET,PUT,POST,DELETE";
            ExposedHeaders = "Location";
            AllowCredentials = true;
        }

        public override void Sanitize()
        {
        }
    }
}
