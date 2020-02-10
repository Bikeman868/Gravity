﻿using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    public class ResponseHeaderConfiguration
    {
        [JsonProperty("name")]
        public string HeaderName { get; set; }

        [JsonProperty("value")]
        public string HeaderValue { get; set; }
    }
}