using System;
using System.Collections.Generic;
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

        private DashboardConfiguration[] _dashboardConfigurations;

        public DiagramGenerator(
            IConfiguration configuration,
            IRequestListener requestListener,
            INodeGraph nodeGraph)
        {
            _requestListener = requestListener;
            _nodeGraph = nodeGraph;

            _dashboardConfig = configuration.Register<DashboardConfiguration[]>(
                "/gravity/ui/dashboards",
                c =>
                {
                    if (c != null)
                        foreach (var dashboardConfiguration in c)
                            dashboardConfiguration.Sanitize();
                    _dashboardConfigurations = c;
                },
                null);
        }

        public DrawingElement GenerateDashboardDrawing(string dashboardName)
        {
            var configurationException = _nodeGraph.ConfigurationException;
            if (configurationException != null)
            {
                return ErrorMessageDrawing(new[]
                {
                    "# Exception thrown by node graph configuration",
                    configurationException.Message,
                    configurationException.StackTrace
                });
            }

            var graphNodes = _nodeGraph.GetNodes(n => n);
            if (graphNodes == null || graphNodes.Length == 0)
            {
                return ErrorMessageDrawing(new[]
                {
                    "# Empty node graph",
                    "You must configure a node graph to process any requests"
                });
            }

            if (_dashboardConfigurations == null || _dashboardConfigurations.Length == 0)
            {
                return ErrorMessageDrawing(new[]
                {
                    "# No dashboards",
                    "You should configure at least one dashboard so that you can see the status of the load balancer"
                });
            }

            var dashboardConfiguration = _dashboardConfigurations.FirstOrDefault(
                c => string.Equals(dashboardName, c.Name, StringComparison.OrdinalIgnoreCase)) 
                ?? _dashboardConfigurations[0];

            return new DashboardDrawing(
                dashboardConfiguration,
                _requestListener,
                graphNodes);
        }

        public DrawingElement GenerateNodeDrawing(string nodeName)
        {
            if (_dashboardConfigurations == null || _dashboardConfigurations.Length == 0) return null;

            var nodes = _nodeGraph.GetNodes(n => n, n => string.Equals(n.Name, nodeName, StringComparison.OrdinalIgnoreCase));
            if (nodes.Length == 0) return null;

            // TODO: Node drawing should be specific to the dashboard that was clicked to get to the node drawing
            var dashboardConfiguration = _dashboardConfigurations[0];
            var nodeDrawingConfig = dashboardConfiguration.Nodes.FirstOrDefault(n => string.Equals(n.NodeName, nodeName, StringComparison.OrdinalIgnoreCase));

            return new DashboardNodeDrawing(
                dashboardConfiguration,
                nodeDrawingConfig,
                nodes[0]);
        }

        public DrawingElement ErrorMessageDrawing(IEnumerable<string> lines)
        {
            var drawing = new RectangleDrawing
            {
                CssClass = "error",
                BottomMargin = 10,
                TopMargin = 10,
                LeftMargin = 10,
                RightMargin = 10
            };

            // TODO: use markdown headings to format the text

            var text = new TextDrawing { Text = lines.ToArray() };
            drawing.AddChild(text);

            return drawing;
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