using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.SpecialPurpose
{
    internal class CorsNode: INode
    {
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public string OutputNode { get; set; }
        public string WebsiteOrigin { get; set; }
        public string AllowedOrigins { get; set; }
        public string AllowedHeaders { get; set; }
        public string AllowedMethods { get; set; }
        public bool AllowCredentials { get; set; }
        public TimeSpan MaxAge { get; set; }
        public string ExposedHeaders { get; set; }
        public bool Offline { get; private set; }

        private INode _nextNode;
        private Regex _allowedOriginsRegex;

        public void Dispose()
        {
        }

        void INode.Bind(INodeGraph nodeGraph)
        {
            _nextNode = nodeGraph.NodeByName(OutputNode);
            _allowedOriginsRegex = new Regex(AllowedOrigins, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        void INode.UpdateStatus()
        {
            if (Disabled || _nextNode == null)
                Offline = true;
            else
                Offline = _nextNode.Offline;
        }

        Task INode.ProcessRequest(IOwinContext context, ILog log)
        {
            if (_nextNode == null)
            {
                context.Response.StatusCode = 503;
                context.Response.ReasonPhrase = "CORS node " + Name + " has no downstream";
                return context.Response.WriteAsync(string.Empty);
            }

            if (!Disabled)
            {
                var origin = context.Request.Headers["Origin"];
                var handled = false;

                var isCrossOrigin =
                    !string.IsNullOrEmpty(origin) &&
                    !origin.Equals(WebsiteOrigin, StringComparison.OrdinalIgnoreCase);

                if (context.Request.Method == "OPTIONS")
                {
                    if (string.IsNullOrEmpty(origin))
                    {
                        context.Response.StatusCode = 403;
                    }
                    else
                    {
                        if (!isCrossOrigin || _allowedOriginsRegex.IsMatch(origin))
                        {
                            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                        }
                        else
                        {
                            context.Response.Headers["Access-Control-Allow-Origin"] = WebsiteOrigin;
                        }

                        context.Response.Headers["Access-Control-Allow-Headers"] = AllowedHeaders;
                        context.Response.Headers["Access-Control-Allow-Methods"] = AllowedMethods;
                        context.Response.Headers["Access-Control-Max-Age"] = ((int)MaxAge.TotalSeconds).ToString();;
                        context.Response.Headers["Access-Control-Allow-Credentials"] = AllowCredentials.ToString().ToLower();
                    }
                    handled = true;
                }

                if (isCrossOrigin)
                {
                    if (context.Request.Method == "POST" ||
                        context.Request.Method == "PUT" ||
                        context.Request.Method == "DELETE")
                    {
                        // Note that the browser will never send this header in a cross-site request
                        // without first obtaining permission from the server using a pre-flight CORS check.
                        if (!string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.StatusCode = 403;
                            handled = true;
                        }
                    }

                    if (!handled && !string.IsNullOrEmpty(origin) && _allowedOriginsRegex.IsMatch(origin))
                    {
                        context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                        context.Response.Headers["Access-Control-Allow-Credentials"] = AllowCredentials.ToString().ToLower();
                        context.Response.Headers["Access-Control-Expose-Headers"] = ExposedHeaders;
                    }
                }

                if (handled)
                {
                    return context.Response.WriteAsync(string.Empty);
                }
            }

            return _nextNode.ProcessRequest(context, log);
        }
    }
}