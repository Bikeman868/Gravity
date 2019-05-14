using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class ListenerConfiguration
    {
       [JsonProperty("disabled")]
        public bool Disabled { get; set; }

        [JsonProperty("paths")]
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
                            IpAddress = "*",
                            PortNumber = 0,
                            NodeName = "A"
                        }
                    };
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