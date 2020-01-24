using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class RouterGroupConfiguration
    {
        [JsonProperty("disabled")]
        public bool Disabled { get; set; }

        [JsonProperty("logic")]
        public ConditionLogic ConditionLogic { get; set; }

        [JsonProperty("conditions")]
        public RouterConditionConfiguration[] Conditions { get; set; }

        [JsonProperty("groups")]
        public RouterGroupConfiguration[] Groups { get; set; }
    }
}