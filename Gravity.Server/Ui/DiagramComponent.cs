using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Gravity.Server.Ui.Shapes;
using OwinFramework.Pages.Core.Enums;
using OwinFramework.Pages.Html.Elements;
using OwinFramework.Pages.Core.Interfaces.Builder;
using OwinFramework.Pages.Core.Interfaces.Runtime;
using Svg;

namespace Gravity.Server.Ui
{
    internal abstract class DiagramComponent: Component
    {
        public const float SvgTextHeight = 12;
        public const float SvgTextLineSpacing = 15;
        public const float SvgTextCharacterSpacing = 6.3f;

        protected DiagramComponent(
            IComponentDependenciesFactory dependencies) 
            : base(dependencies)
        {
        }

        public override IWriteResult WritePageArea(IRenderContext context, PageArea pageArea)
        {
            if (pageArea == PageArea.Body)
            {
                var rootElement = DrawDiagram();
                Write(rootElement, context.Html);
            }

            return base.WritePageArea(context, pageArea);
        }

        protected abstract DrawingElement DrawDiagram();

        private void Write(DrawingElement rootElement, IHtmlWriter writer)
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

            string svg;
            using (var stream = new MemoryStream())
            {
                svgDocument.Write(stream);
                svg = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
                svg = svg.Substring(svg.IndexOf("<svg", StringComparison.OrdinalIgnoreCase));
            }

            writer.GetTextWriter().Write(svg);
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