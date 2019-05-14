using Gravity.Server.DataStructures;
using Gravity.Server.ProcessingNodes;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class RouterRuleConfiguration
    {
        [JsonProperty("disabled")]
        public bool Disabled { get; set; }

        [JsonProperty("condition")]
        public string Condition { get; set; }
    }
}