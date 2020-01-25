﻿using System;
using System.Collections.Generic;
using System.Linq;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using Gravity.Server.ProcessingNodes.LoadBalancing;
using Gravity.Server.ProcessingNodes.Logging;
using Gravity.Server.ProcessingNodes.Routing;
using Gravity.Server.ProcessingNodes.Server;
using Gravity.Server.ProcessingNodes.SpecialPurpose;
using Gravity.Server.ProcessingNodes.Transform;
using Gravity.Server.Ui.Nodes;
using Gravity.Server.Utility;

namespace Gravity.Server.Ui.Drawings
{
    internal class DashboardDrawing : NodeTile
    {
        public DashboardDrawing(
            DashboardConfiguration dashboardConfiguration,
            IRequestListener requestListener,
            INode[] nodes)
            : base(null, dashboardConfiguration.Name + " Dashboard", "drawing", false, 1)
        {
            LeftMargin = 20;
            RightMargin = 20;
            TopMargin = 20;
            BottomMargin = 20;

            var listenerDrawings = new List<ListenerTile>();
            var nodeDrawings = new DefaultDictionary<string, NodeTile>(StringComparer.OrdinalIgnoreCase);

            var listeners = requestListener.Endpoints;
            if (listeners != null)
            {
                foreach (var listener in listeners)
                {
                    var listenerName = listener.Name;

                    var listenerDrawingConfig = dashboardConfiguration.Listeners.FirstOrDefault(n => n.NodeName == listenerName);
                    if (listenerDrawingConfig == null) continue;

                    var listenerDrawing = new ListenerTile(
                        this,
                        listener,
                        dashboardConfiguration.TrafficIndicator)
                    {
                        Left = listenerDrawingConfig.X,
                        Top = listenerDrawingConfig.Y,
                        Width = listenerDrawingConfig.Width,
                        Height = listenerDrawingConfig.Height
                    };

                    AddChild(listenerDrawing);

                    listenerDrawings.Add(listenerDrawing);
                }
            }

            if (nodes != null && nodes.Length > 0)
            {
                foreach (var node in nodes)
                {
                    var nodeName = node.Name;

                    var nodeDrawingConfig = dashboardConfiguration.Nodes.FirstOrDefault(n => n.NodeName == nodeName);
                    if (nodeDrawingConfig == null) continue;

                    NodeTile nodeDrawing;

                    var internalRequest = node as InternalNode;
                    var response = node as ResponseNode;
                    var roundRobin = node as RoundRobinNode;
                    var router = node as RoutingNode;
                    var server = node as ServerNode;
                    var stickySession = node as StickySessionNode;
                    var transform = node as TransformNode;
                    var leastConnections = node as LeastConnectionsNode;
                    var cors = node as CorsNode;
                    var changeLogFilter = node as ChangeLogFilterNode;

                    if (internalRequest != null) nodeDrawing = new InternalRequestTile(this, internalRequest, nodeDrawingConfig);
                    else if (response != null) nodeDrawing = new ResponseTile(this, response, nodeDrawingConfig);
                    else if (roundRobin != null) nodeDrawing = new RoundRobinTile(this, roundRobin, nodeDrawingConfig, dashboardConfiguration.TrafficIndicator);
                    else if (router != null) nodeDrawing = new RouterTile(this, router, nodeDrawingConfig, dashboardConfiguration.TrafficIndicator);
                    else if (server != null) nodeDrawing = new ServerTile(this, server, nodeDrawingConfig);
                    else if (stickySession != null) nodeDrawing = new StickySessionTile(this, stickySession, nodeDrawingConfig, dashboardConfiguration.TrafficIndicator);
                    else if (transform != null) nodeDrawing = new TransformTile(this, transform, nodeDrawingConfig);
                    else if (leastConnections != null) nodeDrawing = new LeastConnectionsTile(this, leastConnections, nodeDrawingConfig, dashboardConfiguration.TrafficIndicator);
                    else if (cors != null) nodeDrawing = new CorsTile(this, cors, nodeDrawingConfig);
                    else if (changeLogFilter != null) nodeDrawing = new ChangeLogFilterTile(this, changeLogFilter, nodeDrawingConfig);
                    else nodeDrawing = new NodeTile(this, node.Name, "", true);

                    nodeDrawing.Left = nodeDrawingConfig.X;
                    nodeDrawing.Top = nodeDrawingConfig.Y;
                    nodeDrawing.Width = nodeDrawingConfig.Width;
                    nodeDrawing.Height = nodeDrawingConfig.Height;

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