using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    public class NodeConfiguration
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("disabled")]
        public bool Disabled { get; set; }

        [JsonIgnore()] 
        public INode Node;

        public virtual void Sanitize()
        {
        }
    }
}