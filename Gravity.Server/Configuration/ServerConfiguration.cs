using System;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class ServerConfiguration: NodeConfiguration
    {
        /// <summary>
        /// Host name or IP address of the server
        /// </summary>
        [JsonProperty("host")]
        public string Host { get; set; }

        /// <summary>
        /// Port number of the server or null to pass through from the request
        /// </summary>
        [JsonProperty("port")]
        public int? Port { get; set; }

        /// <summary>
        /// How long to wait for a connection to be opened
        /// </summary>
        [JsonProperty("connectionTimeout")]
        public TimeSpan ConnectionTimeout { get; set; }

        /// <summary>
        /// How long to wait for the server to respond to a request
        /// </summary>
        [JsonProperty("responseTimeout")]
        public TimeSpan ResponseTimeout { get; set; }

        /// <summary>
        /// The path of the URL to send as a health check
        /// </summary>
        [JsonProperty("healthCheckMethod")]
        public string HealthCheckMethod { get; set; }

        /// <summary>
        /// The path of the URL to send as a health check
        /// </summary>
        [JsonProperty("healthCheckHost")]
        public string HealthCheckHost { get; set; }

        /// <summary>
        /// The path of the URL to send as a health check
        /// </summary>
        [JsonProperty("healthCheckPort")]
        public int HealthCheckPort { get; set; }

        /// <summary>
        /// The path of the URL to send as a health check
        /// </summary>
        [JsonProperty("healthCheckPath")]
        public string HealthCheckPath { get; set; }

        [JsonProperty("healthCheckCodes")]
        public int[] HealthCheckCodes { get; set; }

        public ServerConfiguration()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(5);
            ResponseTimeout = TimeSpan.FromMinutes(1);
            HealthCheckMethod = "GET";
            HealthCheckPath = "/";
            HealthCheckPort = 80;
            HealthCheckCodes = new[] { 200 };
        }

        public override void Sanitize()
        {
        }
    }
}