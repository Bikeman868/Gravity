using System.Collections.Generic;
using System.Linq;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class ResponseDrawing: NodeDrawing
    {
        public ResponseDrawing(
            DrawingElement drawing, 
            Response response) 
            : base(drawing, "Response " + response.Name)
        {
            CssClass = "responder";

            var details = new List<string>();

            if (response.Disabled) 
                details.Add("Disabled");

            details.Add(response.StatusCode + " " + response.ReasonPhrase);

            if (response.HeaderNames != null)
            {
                for (var i = 0; i < response.HeaderNames.Length; i++)
                {
                    details.Add(response.HeaderNames[i] + ": " + response.HeaderValues[i]);
                }
            }

            if (!string.IsNullOrWhiteSpace(response.Content))
                details.Add(response.Content);

            AddDetails(details);
        }
    }
}