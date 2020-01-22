﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Gravity.Server.Configuration;
using Gravity.Server.Interfaces;
using Gravity.Server.Ui.Shapes;
using OwinFramework.Interfaces.Builder;
using Svg;

namespace Gravity.Server.Ui.Drawings
{
    internal class DiagramGenerator : IDrawingGenerator
    {
        public const float SvgTextHeight = 12;
        public const float SvgTextLineSpacing = 15;
        public const float SvgTextCharacterSpacing = 6.3f;

        private readonly IRequestListener _requestListener;
        private readonly INodeGraph _nodeGraph;

        private readonly IDisposable _dashboardConfig;

        private DashboardConfiguration _dashboardConfiguration;

        public DiagramGenerator(
            IConfiguration configuration,
            IRequestListener requestListener,
            INodeGraph nodeGraph)
        {
            _requestListener = requestListener;
            _nodeGraph = nodeGraph;

            _dashboardConfig = configuration.Register(
                "/gravity/ui/dashboard",
                c => _dashboardConfiguration = c.Sanitize(),
                new DashboardConfiguration());
        }

        public DrawingElement GenerateDashboardDrawing()
        {
            return new DashboardDrawing(
                _dashboardConfiguration,
                _requestListener,
                _nodeGraph.GetNodes(n => n));
        }

        public DrawingElement GenerateNodeDrawing(string nodeName)
        {
            var nodes = _nodeGraph.GetNodes(n => n, n => string.Equals(n.Name, nodeName, StringComparison.OrdinalIgnoreCase));
            if (nodes.Length == 0) return null;

            var nodeDrawingConfig = _dashboardConfiguration.Nodes.FirstOrDefault(n => string.Equals(n.NodeName, nodeName, StringComparison.OrdinalIgnoreCase));

            return new DashboardNodeDrawing(
                _dashboardConfiguration,
                nodeDrawingConfig,
                nodes[0]);
        }

        public SvgDocument ProduceSvg(DrawingElement rootElement)
        {
            if (rootElement == null)
            {
                return new SvgDocument
                {
                    FontFamily = "Arial",
                    FontSize = SvgTextHeight
                };
            }

            rootElement.SortDescendentsByZOrder();
            rootElement.Arrange();
            rootElement.UpdateConnectedElements();
            rootElement.ArrangeMargins();
            var drawing = rootElement.Draw();

            var svgDocument = new SvgDocument
            {
                FontFamily = "Arial",
                FontSize = SvgTextHeight
            };

            var styles = GetTextResource("drawing.css");
            if (!string.IsNullOrEmpty(styles))
            {
                var styleElement = new NonSvgElement("style")
                {
                    Content = "\n" + styles
                };
                svgDocument.Children.Add(styleElement);
            }

            var script = GetTextResource("drawing.js");
            if (!string.IsNullOrEmpty(script))
            {
                svgDocument.CustomAttributes.Add("onload", "init(evt)");
                var scriptElement = new NonSvgElement("script");
                scriptElement.CustomAttributes.Add("type", "text/ecmascript");
                scriptElement.Content = "\n" + script;
                svgDocument.Children.Add(scriptElement);
            }

            svgDocument.Children.Add(drawing);

            svgDocument.Width = rootElement.Left + rootElement.Width;
            svgDocument.Height = rootElement.Top + rootElement.Height;
            svgDocument.ViewBox = new SvgViewBox(0, 0, svgDocument.Width, svgDocument.Height);

            return svgDocument;
        }

        #region Embedded resources

        private string GetTextResource(string filename)
        {
            var scriptResourceName = Assembly.GetExecutingAssembly().GetManifestResourceNames().FirstOrDefault(n => n.Contains(filename));
            if (scriptResourceName != null)
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(scriptResourceName))
                {
                    if (stream == null) return null;
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            return null;
        }

        #endregion
    }
}