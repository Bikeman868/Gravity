using System;
using System.Net;
using Gravity.Server.Pipeline;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class ListenerEndpointConfiguration
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("disabled")]
        public bool Disabled { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("ipAddress")]
        public string IpAddress { get; set; }

        [JsonProperty("port")]
        public int PortNumber { get; set; }

        [JsonProperty("node")]
        public string NodeName { get; set; }

        [JsonIgnore]
        public NodeOutput ProcessingNode { get; set; }

        public ListenerEndpointConfiguration Sanitize()
        {
            if (PortNumber > 65535) throw new ArgumentOutOfRangeException("PortNumber", PortNumber, "port number must be less than 65536");

            if (IpAddress != "*")
            {
                if (!IPAddress.TryParse(IpAddress, out var ipAddress))
                    throw new ArgumentOutOfRangeException("IpAddress", IpAddress, "invalid IP address format");
            }

            return this;
        }
    }
}