using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.ProcessingNodes;
using Gravity.Server.Ui.Nodes;
using Gravity.Server.Ui.Shapes;
using OwinFramework.Interfaces.Builder;
using OwinFramework.Pages.Core.Attributes;
using OwinFramework.Pages.Core.Debug;
using OwinFramework.Pages.Core.Enums;
using OwinFramework.Pages.Core.Interfaces.Builder;
using OwinFramework.Pages.Core.Interfaces.Runtime;
using OwinFramework.Pages.Html.Elements;
using OwinFramework.Pages.Html.Runtime;

namespace Gravity.Server.Ui
{
    [IsComponent("dashboard_diagram")]
    internal class DashboardDiagramComponent: DiagramComponent
    {
        private readonly INodeGraph _nodeGraph;

        private readonly IDisposable _listenerConfig;
        private readonly IDisposable _dashboardConfig;

        private ListenerConfiguration _listenerConfiguration;
        private DashboardConfiguration _dashboardConfiguration;

        public DashboardDiagramComponent(
            IComponentDependenciesFactory dependencies,
            IConfiguration configuration,
            INodeGraph nodeGraph) 
            : base(dependencies)
        {
            _nodeGraph = nodeGraph;

            _listenerConfig = configuration.Register(
                "/gravity/listener", 
                c => _listenerConfiguration = c.Sanitize(), 
                new ListenerConfiguration());

            _dashboardConfig = configuration.Register(
                "/gravity/dashboard",
                c => _dashboardConfiguration = c.Sanitize(),
                new DashboardConfiguration());
        }

        protected override DrawingElement DrawDiagram()
        {
            return new DashboardDrawing(
                _dashboardConfiguration,
                _listenerConfiguration,
                _nodeGraph.GetNodes(n => n));
        }

        private class DashboardDrawing : NodeDrawing
        {
            public DashboardDrawing(
                    DashboardConfiguration dashboardConfiguration,
                    ListenerConfiguration listenerConfiguration,
                    INode[] nodes)
                : base(null, "Dashboard", 1)
            {
                LeftMargin = 20;
                RightMargin = 20;
                TopMargin = 20;
                BottomMargin = 20;

                CssClass = "drawing";

                var listenerDrawings = new List<ListenerDrawing>();
                var nodeDrawings = new Dictionary<string, NodeDrawing>(StringComparer.OrdinalIgnoreCase);

                if (listenerConfiguration.Endpoints != null)
                {
                    var x = dashboardConfiguration.Listeners.X;
                    var y = dashboardConfiguration.Listeners.Y;

                    foreach (var endpoint in listenerConfiguration.Endpoints)
                    {
                        var listenerDrawing = new ListenerDrawing(this, endpoint)
                        {
                            Left = x,
                            Top = y,
                            Width = 200,
                            Height = 80
                        };

                        AddChild(listenerDrawing);

                        listenerDrawings.Add(listenerDrawing);

                        x += dashboardConfiguration.Listeners.XSpacing;
                        y += dashboardConfiguration.Listeners.YSpacing;
                    }
                }

                if (nodes != null && nodes.Length > 0)
                {
                    foreach (var node in nodes)
                    {
                        NodeDrawing nodeDrawing;

                        var internalRequest = node as InternalPage;
                        var response = node as Response;
                        var roundRobin = node as RoundRobinBalancer;
                        var router = node as RoutingNode;
                        var server = node as ServerEndpoint;
                        var stickySession = node as StickySessionBalancer;
                        var transform = node as Transform;

                        if (internalRequest != null) nodeDrawing = new InternalRequestDrawing(this, internalRequest);
                        else if (response != null) nodeDrawing = new ResponseDrawing(this, response);
                        else if (roundRobin != null) nodeDrawing = new RoundRobbinDrawing(this, roundRobin);
                        else if (router != null) nodeDrawing = new RouterDrawing(this, router);
                        else if (server != null) nodeDrawing = new ServerDrawing(this, server);
                        else if (stickySession != null) nodeDrawing = new StickySessionDrawing(this, stickySession);
                        else if (transform != null) nodeDrawing = new TransformDrawing(this, transform);
                        else nodeDrawing = new NodeDrawing(this, node.Name);

                        var nodeName = node.Name;
                        var nodeDrawingConfig = dashboardConfiguration.Nodes.FirstOrDefault(n => n.NodeName == nodeName);

                        if (nodeDrawingConfig != null)
                        {
                            nodeDrawing.Left = nodeDrawingConfig.X;
                            nodeDrawing.Top = nodeDrawingConfig.Y;
                            nodeDrawing.Width = nodeDrawingConfig.Width;
                            nodeDrawing.Height = nodeDrawingConfig.Height;
                        }

                        AddChild(nodeDrawing);
                        nodeDrawings[node.Name] = nodeDrawing;
                    }
                }

                foreach (var listenerDrawing in listenerDrawings)
                    listenerDrawing.AddLines(nodeDrawings);

                foreach (var nodeDrawing in nodeDrawings.Values)
                    nodeDrawing.AddLines(nodeDrawings);
            }

            protected override void ArrangeChildren()
            {
                ArrangeChildrenInFixedPositions();
            }
        }
    }
}