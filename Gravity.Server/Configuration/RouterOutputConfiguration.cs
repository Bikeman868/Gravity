using Gravity.Server.DataStructures;
using Gravity.Server.ProcessingNodes;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class RouterOutputConfiguration
    {
        [JsonProperty("routeTo")]
        public string RouteTo { get; set; }

        [JsonProperty("rules")]
        public RouterRuleConfiguration[] Rules { get; set; }

        [JsonProperty("ruleLogic")]
        public RuleLogic RuleLogic { get; set; }
    }
}