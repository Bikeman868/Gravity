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
       [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("paths")]
        public ListeningEndpoint[] Endpoints { get; set; }

        public ListenerConfiguration()
        {
            Enabled = true;
        }

        public ListenerConfiguration Sanitize()
        {
            try
            {
                if (Endpoints == null)
                {
                    Endpoints = new[]
                    {
                        new ListeningEndpoint
                        {
                            Enabled = true,
                            IpAddress = "*",
                            PortNumber = 52581,
                            NodeName = "A"
                        }
                    };
                }
            }
            catch
            {
                Enabled = false;
                throw;
            }
            return this;
        }
    }
}