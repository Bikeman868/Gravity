using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class ServerDrawing: NodeDrawing
    {
        public ServerDrawing(
            DrawingElement drawing, 
            ServerEndpoint server) 
            : base(drawing, "Server", 2, server.Name)
        {
            CssClass = "server";
        }
    }
}