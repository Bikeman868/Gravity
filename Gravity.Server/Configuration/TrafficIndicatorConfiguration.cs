using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    public class TrafficIndicatorConfiguration
    {
        /// <summary>
        /// The requests/min at which the lines go the next thickness
        /// </summary>
        [JsonProperty("thresholds")]
        public double[] Thresholds { get; set; }

        public void Sanitize()
        {
            if (Thresholds == null || Thresholds.Length != 4)
                Thresholds = new[]{ 1d, 5d, 50d, 200d };
        }
    }
}