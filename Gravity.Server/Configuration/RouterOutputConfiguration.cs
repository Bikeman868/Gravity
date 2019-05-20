using Gravity.Server.Utility;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class RouterOutputConfiguration
    {
        [JsonProperty("to")]
        public string RouteTo { get; set; }

        [JsonProperty("conditions")]
        public RouterRuleConfiguration[] Rules { get; set; }

        [JsonProperty("logic")]
        public RuleLogic RuleLogic { get; set; }

        [JsonIgnore]
        public NodeOutput ProcessingNode { get; set; }
    }
}