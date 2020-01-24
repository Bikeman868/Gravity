﻿using System.Collections.Generic;
using Gravity.Server.Configuration;
using Gravity.Server.ProcessingNodes.SpecialPurpose;
using Gravity.Server.Ui.Shapes;

namespace Gravity.Server.Ui.Nodes
{
    internal class ResponseTile: NodeTile
    {
        public ResponseTile(
            DrawingElement drawing, 
            ResponseNode response,
            DashboardConfiguration.NodeConfiguration nodeConfiguration) 
            : base(
                drawing,
                nodeConfiguration?.Title ?? "Response", 
                "responder", 
                response.Offline, 
                2, 
                response.Name)
        {
            LinkUrl = "/ui/node?name=" + response.Name;

            var details = new List<string>();

            details.Add(response.StatusCode + " " + response.ReasonPhrase);

            if (response.HeaderNames != null)
            {
                for (var i = 0; i < response.HeaderNames.Length; i++)
                {
                    details.Add(response.HeaderNames[i] + ": " + response.HeaderValues[i]);
                }
            }

            if (!string.IsNullOrEmpty(response.ContentFile))
                details.Add("[" + response.ContentFile + "]");
            else if (!string.IsNullOrWhiteSpace(response.Content))
                details.Add(response.Content);

            AddDetails(details, null, response.Offline ? "disabled" : string.Empty);
        }
    }
}