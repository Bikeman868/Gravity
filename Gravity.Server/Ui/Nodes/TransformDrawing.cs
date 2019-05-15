using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class TransformDrawing: NodeDrawing
    {
        public TransformDrawing(
            DrawingElement drawing, 
            Transform transform) 
            : base(drawing, "Transform", 2, transform.Name)
        {
            CssClass = transform.Disabled ? "disabled" : "transform";
        }
    }
}