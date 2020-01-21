﻿using System;
using System.Collections.Generic;
using System.Linq;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using Gravity.Server.ProcessingNodes.LoadBalancing;
using Gravity.Server.ProcessingNodes.Routing;
using Gravity.Server.ProcessingNodes.Server;
using Gravity.Server.ProcessingNodes.SpecialPurpose;
using Gravity.Server.ProcessingNodes.Transform;
using Gravity.Server.Ui.Nodes;
using Gravity.Server.Utility;

namespace Gravity.Server.Ui.Drawings
{
    internal class DashboardDrawing : NodeDrawing
    {
        public DashboardDrawing(
            DashboardConfiguration dashboardConfiguration,
            IRequestListener requestListener,
            INode[] nodes)
            : base(null, "Dashboard", "drawing", false, 1)
        {
            LeftMargin = 20;
            RightMargin = 20;
            TopMargin = 20;
            BottomMargin = 20;

            var listenerDrawings = new List<ListenerDrawing>();
            var nodeDrawings = new DefaultDictionary<string, NodeDrawing>(StringComparer.OrdinalIgnoreCase);

            var endpoints = requestListener.Endpoints;
            if (endpoints != null)
            {
                var x = dashboardConfiguration.Listeners.X;
                var y = dashboardConfiguration.Listeners.Y;

                foreach (var endpoint in endpoints)
                {
                    var listenerDrawing = new ListenerDrawing(
                        this, 
                        endpoint, 
                        null,
                        dashboardConfiguration.TrafficIndicator)
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

                    var internalRequest = node as InternalNode;
                    var response = node as ResponseNode;
                    var roundRobin = node as RoundRobinNode;
                    var router = node as RoutingNode;
                    var server = node as ServerNode;
                    var stickySession = node as StickySessionNode;
                    var transform = node as TransformNode;
                    var leastConnections = node as LeastConnectionsNode;
                    var cors = node as CorsNode;

                    var nodeName = node.Name;
                    var nodeDrawingConfig = dashboardConfiguration.Nodes.FirstOrDefault(n => n.NodeName == nodeName);

                    if (internalRequest != null) nodeDrawing = new InternalRequestDrawing(this, internalRequest, nodeDrawingConfig);
                    else if (response != null) nodeDrawing = new ResponseDrawing(this, response, nodeDrawingConfig);
                    else if (roundRobin != null) nodeDrawing = new RoundRobinDrawing(this, roundRobin, nodeDrawingConfig, dashboardConfiguration.TrafficIndicator);
                    else if (router != null) nodeDrawing = new RouterDrawing(this, router, nodeDrawingConfig, dashboardConfiguration.TrafficIndicator);
                    else if (server != null) nodeDrawing = new ServerDrawing(this, server, nodeDrawingConfig);
                    else if (stickySession != null) nodeDrawing = new StickySessionDrawing(this, stickySession, nodeDrawingConfig, dashboardConfiguration.TrafficIndicator);
                    else if (transform != null) nodeDrawing = new TransformDrawing(this, transform, nodeDrawingConfig);
                    else if (leastConnections != null) nodeDrawing = new LeastConnectionsDrawing(this, leastConnections, nodeDrawingConfig, dashboardConfiguration.TrafficIndicator);
                    else if (cors != null) nodeDrawing = new CorsDrawing(this, cors, nodeDrawingConfig);
                    else nodeDrawing = new NodeDrawing(this, node.Name, "", true);

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