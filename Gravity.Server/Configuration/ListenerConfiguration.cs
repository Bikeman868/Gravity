using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    public class ListenerConfiguration
    {
       [JsonProperty("disabled")]
        public bool Disabled { get; set; }

        [JsonProperty("endpoints")]
        public ListenerEndpointConfiguration[] Endpoints { get; set; }

        public ListenerConfiguration Sanitize()
        {
            try
            {
                if (Endpoints == null)
                {
                    Endpoints = new[]
                    {
                        // Default configuration is to send all traffic to node "A"
                        new ListenerEndpointConfiguration
                        {
                            Name = "Input",
                            IpAddress = "*",
                            PortNumber = 0,
                            NodeName = "A"
                        }
                    };
                }
                else
                {
                    for (var i = 0; i < Endpoints.Length; i++)
                        Endpoints[i] = Endpoints[i].Sanitize();
                }
            }
            catch
            {
                Disabled = true;
                throw;
            }
            return this;
        }
    }
}