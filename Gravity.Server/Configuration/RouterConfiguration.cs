using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class RouterConfiguration: NodeConfiguration
    {
        [JsonProperty("routes")]
        public RouterOutputConfiguration[] Outputs { get; set; }

        public override void Sanitize()
        {
        }
    }
}