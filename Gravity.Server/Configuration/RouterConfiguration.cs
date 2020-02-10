using Newtonsoft.Json;

namespace Gravity.Server.Configuration
{
    public class RouterConfiguration: NodeConfiguration
    {
        [JsonProperty("routes")]
        public RouterOutputConfiguration[] Outputs { get; set; }

        public override void Sanitize()
        {
            if (Outputs == null || Outputs.Length == 0)
            {
                Outputs = new[]
                {
                    new RouterOutputConfiguration { RouteTo = "A" }
                };
            }
            else
            {
                foreach (var output in Outputs)
                    output.Sanitize();
            }
        }
    }
}