using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;

namespace Gravity.Server.ProcessingNodes.SpecialPurpose
{
    internal class CorsNode: ProcessingNode
    {
        public string OutputNode { get; set; }
        public string WebsiteOrigin { get; set; }
        public string AllowedOrigins { get; set; }
        public string AllowedHeaders { get; set; }
        public string AllowedMethods { get; set; }
        public bool AllowCredentials { get; set; }
        public TimeSpan MaxAge { get; set; }
        public string ExposedHeaders { get; set; }

        private INode _nextNode;
        private Regex _allowedOriginsRegex;

        public override void Bind(INodeGraph nodeGraph)
        {
            _nextNode = nodeGraph.NodeByName(OutputNode);
            _allowedOriginsRegex = new Regex(AllowedOrigins, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Offline = true;
        }

        public override void UpdateStatus()
        {
            if (Disabled || _nextNode == null)
                Offline = true;
            else
                Offline = _nextNode.Offline;
        }

        public override Task ProcessRequest(IRequestContext context)
        {
            if (_nextNode == null)
            {
                return Task.Run(() =>
                {
                    context.Outgoing.StatusCode = 503;
                    context.Outgoing.ReasonPhrase = "CORS node " + Name + " has no downstream";
                    context.Outgoing.SendHeaders(context);
                });
            }

            if (!Disabled)
            {
                if (!context.Incoming.Headers.TryGetValue("Origin", out var origins))
                    origins = new string[] { null };
                var origin = origins[0];

                var handled = false;

                var isCrossOrigin =
                    !string.IsNullOrEmpty(origin) &&
                    !origin.Equals(WebsiteOrigin, StringComparison.OrdinalIgnoreCase);

                if (context.Incoming.Method == "OPTIONS")
                {
                    if (string.IsNullOrEmpty(origin))
                    {
                        context.Outgoing.StatusCode = 403;
                    }
                    else
                    {
                        if (!isCrossOrigin || _allowedOriginsRegex.IsMatch(origin))
                        {
                            context.Outgoing.Headers["Access-Control-Allow-Origin"] = new [] { origin };
                        }
                        else
                        {
                            context.Outgoing.Headers["Access-Control-Allow-Origin"] = new[] { WebsiteOrigin };
                        }

                        context.Outgoing.Headers["Access-Control-Allow-Headers"] = new[] { AllowedHeaders };
                        context.Outgoing.Headers["Access-Control-Allow-Methods"] = new[] { AllowedMethods };
                        context.Outgoing.Headers["Access-Control-Max-Age"] = new[] {((int) MaxAge.TotalSeconds).ToString()};
                        context.Outgoing.Headers["Access-Control-Allow-Credentials"] = new[] { AllowCredentials.ToString().ToLower() };
                    }
                    handled = true;
                }

                if (isCrossOrigin)
                {
                    if (context.Incoming.Method == "POST" ||
                        context.Incoming.Method == "PUT" ||
                        context.Incoming.Method == "DELETE")
                    {
                        // Note that the browser will never send this header in a cross-site request
                        // without first obtaining permission from the server using a pre-flight CORS check.
                        if (!context.Incoming.Headers.TryGetValue("X-Requested-With", out var requestedWithHeaders) ||
                            !string.Equals(requestedWithHeaders[0], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Outgoing.StatusCode = 403;
                            handled = true;
                        }
                    }

                    if (!handled && !string.IsNullOrEmpty(origin) && _allowedOriginsRegex.IsMatch(origin))
                    {
                        context.Outgoing.Headers["Access-Control-Allow-Origin"] = new[] { origin };
                        context.Outgoing.Headers["Access-Control-Allow-Credentials"] = new[] { AllowCredentials.ToString().ToLower() };
                        context.Outgoing.Headers["Access-Control-Expose-Headers"] = new[] { ExposedHeaders };
                    }
                }

                if (handled)
                {
                    return Task.Run(() => context.Outgoing.SendHeaders(context));
                }
            }

            return _nextNode.ProcessRequest(context);
        }
    }
}