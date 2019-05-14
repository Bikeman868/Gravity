using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Gravity.Server.Configuration;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class RouterDrawing: NodeDrawing
    {
        public RouterDrawing(
            DrawingElement page, 
            RouterConfiguration router) 
            : base(page, "Router " + router.Name)
        {
            CssClass = "router";
        }
    }
}