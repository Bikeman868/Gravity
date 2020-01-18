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
        /// How long to wait for the server to send the next batch of data
        /// before deciding that the response is finished
        /// </summary>
        [JsonProperty("readTimeout")]
        public TimeSpan ReadTimeout { get; set; }

        /// <summary>
        /// Set this to True to pool and reuse connections. This is
        /// especially useful if the communication is HTTPS because
        /// opening a connection involves a negotiation and validation
        /// of the server's SSL certificate
        /// </summary>
        [JsonProperty("reuseConnections")]
        public bool ReuseConnections { get; set; }

        /// <summary>
        /// How frequently to look up the IP addresses of the server in
        /// DNS. Only applies if the host property is not an IP address
        /// </summary>
        [JsonProperty("dnsLookupInterval")]
        public TimeSpan DnsLookupInterval { get; set; }

        /// <summary>
        /// How frequently to update the dashboard stats
        /// </summary>
        [JsonProperty("recalculateInterval")]
        public TimeSpan RecalculateInterval { get; set; }

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
        [JsonProperty("healthCheckLog")]
        public bool HealthCheckLog { get; set; }

        /// <summary>
        /// The path of the URL to send as a health check
        /// </summary>
        [JsonProperty("healthCheckPath")]
        public string HealthCheckPath { get; set; }

        /// <summary>
        /// A list of the HTTP status codes that are considered a healthy response
        /// </summary>
        [JsonProperty("healthCheckCodes")]
        public int[] HealthCheckCodes { get; set; }

        /// <summary>
        /// How frequently to check the health of the server
        /// </summary>
        [JsonProperty("healthCheckInterval")]
        public TimeSpan HealthCheckInterval { get; set; }

        public ServerConfiguration()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(5);
            ResponseTimeout = TimeSpan.FromSeconds(20);
            ReadTimeout = TimeSpan.FromSeconds(3);
            DnsLookupInterval = TimeSpan.FromMinutes(5);
            RecalculateInterval = TimeSpan.FromSeconds(5);
            HealthCheckMethod = "GET";
            HealthCheckPath = "/";
            HealthCheckPort = 80;
            HealthCheckCodes = new[] { 200 };
            HealthCheckLog = false;
            HealthCheckInterval = TimeSpan.FromSeconds(30);
        }

        public override void Sanitize()
        {
        }
    }
}