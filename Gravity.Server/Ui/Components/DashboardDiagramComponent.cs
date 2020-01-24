using System;
using Gravity.Server.Interfaces;
using Gravity.Server.Ui.Shapes;
using OwinFramework.Pages.Core.Attributes;
using OwinFramework.Pages.Core.Enums;
using OwinFramework.Pages.Core.Interfaces.Builder;
using OwinFramework.Pages.Core.Interfaces.Runtime;
using OwinFramework.Pages.Html.Elements;

namespace Gravity.Server.Ui.Components
{
    [IsComponent("dashboard_diagram")]
    [NeedsComponent("page_refresh")]
    internal class DashboardDiagramComponent: DiagramComponentBase
    {
        public DashboardDiagramComponent(
            IComponentDependenciesFactory dependencies,
            IDrawingGenerator drawingGenerator) 
            : base(dependencies, drawingGenerator)
        {
        }

        protected override DrawingElement DrawDiagram(IRenderContext context)
        {
            var dashboardName = context.OwinContext.Request.Query["name"];
            return DiagramGenerator.GenerateDashboardDrawing(dashboardName);
        }
    }
}