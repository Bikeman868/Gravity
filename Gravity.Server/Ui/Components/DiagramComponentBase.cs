﻿using System;
using System.IO;
using System.Text;
using Gravity.Server.Interfaces;
using Gravity.Server.Ui.Shapes;
using OwinFramework.Pages.Core.Enums;
using OwinFramework.Pages.Core.Interfaces.Builder;
using OwinFramework.Pages.Core.Interfaces.Runtime;
using OwinFramework.Pages.Html.Elements;

namespace Gravity.Server.Ui.Components
{
    internal abstract class DiagramComponentBase: Component
    {
        protected readonly IDrawingGenerator DiagramGenerator;

        protected DiagramComponentBase(
            IComponentDependenciesFactory dependencies,
            IDrawingGenerator diagramGenerator) 
            : base(dependencies)
        {
            DiagramGenerator = diagramGenerator;
        }

        public override IWriteResult WritePageArea(IRenderContext context, PageArea pageArea)
        {
            if (pageArea == PageArea.Body)
            {
                var rootElement = DrawDiagram(context);
                Write(rootElement, context.Html);
            }

            return base.WritePageArea(context, pageArea);
        }

        protected abstract DrawingElement DrawDiagram(IRenderContext context);

        private void Write(DrawingElement rootElement, IHtmlWriter writer)
        {
            var svgDocument = DiagramGenerator.ProduceSvg(rootElement);

            string svg;
            using (var stream = new MemoryStream())
            {
                svgDocument.Write(stream);
                svg = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
                svg = svg.Substring(svg.IndexOf("<svg", StringComparison.OrdinalIgnoreCase));
            }

            writer.GetTextWriter().Write(svg);
        }
    }
}
