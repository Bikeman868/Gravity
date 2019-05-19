using System;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class TrafficIndicatorConfiguration
    {
        /// <summary>
        /// The requests/min at which the lines go the next thickness
        /// </summary>
        [JsonProperty("thresholds")]
        public float[] Thresholds { get; set; }

        public void Sanitize()
        {
            if (Thresholds == null || Thresholds.Length != 4)
                Thresholds = new[]{ 1f, 5f, 50f, 200f };
        }
    }
}