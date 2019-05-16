using Gravity.Server.Interfaces;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    internal class NodeConfiguration
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("disabled")]
        public bool Disabled { get; set; }

        [JsonIgnore()] 
        public INode Node;
    }
}