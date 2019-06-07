using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using Gravity.Server.Interfaces;
using Gravity.Server.Ui.Nodes;
using Gravity.Server.Ui.Shapes;
using Svg;

namespace Gravity.Server.Ui
{
    internal class DiagramGenerator : IDiagramGenerator
    {
        public const float SvgTextHeight = 12;
        public const float SvgTextLineSpacing = 15;
        public const float SvgTextCharacterSpacing = 6.3f;

        public DrawingElement GenerateDashboardDiagram()
        {
            throw new NotImplementedException();
        }

        public SvgDocument ProduceSvg(DrawingElement rootElement)
        {
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