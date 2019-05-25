using Gravity.Server.Utility;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class RouterOutputConfiguration
    {
        [JsonProperty("to")]
        public string RouteTo { get; set; }

        [JsonProperty("conditions")]
        public RouterConditionConfiguration[] Conditions { get; set; }

        [JsonProperty("logic")]
        public ConditionLogic ConditionLogic { get; set; }

        [JsonIgnore]
        public NodeOutput ProcessingNode { get; set; }

        public void Sanitize()
        { }
    }
}