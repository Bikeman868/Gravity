using System;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class StickySessionConfiguration: NodeConfiguration
    {
        [JsonProperty("outputs")]
        public string[] Outputs { get; set; }

        [JsonProperty("cookie")]
        public string SesionCookie { get; set; }

        [JsonProperty("sessionDuration")]
        public TimeSpan SessionDuration { get; set; }
    }
}