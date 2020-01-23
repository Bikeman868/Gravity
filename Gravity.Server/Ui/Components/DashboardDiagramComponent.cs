using System;
using OwinFramework.Pages.Core.Attributes;
using OwinFramework.Pages.Core.Enums;
using OwinFramework.Pages.Core.Interfaces.Builder;
using OwinFramework.Pages.Core.Interfaces.Runtime;
using OwinFramework.Pages.Html.Elements;

namespace Gravity.Server.Ui.Components
{
    [IsComponent("dashboard_diagram")]
    [DeployFunction(
        "void",
        "reloadDashboardDiagram",
        "e",
        @"setTimeout(function() { e.src = e.src.replace(/r=.+/, 'r=' + Math.random()); }, 2000);")]
    internal class DashboardDiagramComponent: Component
    {
        public DashboardDiagramComponent(
            IComponentDependenciesFactory dependencies) 
            : base(dependencies)
        {
        }

        public override IWriteResult WritePageArea(IRenderContext context, PageArea pageArea)
        {
            if (pageArea == PageArea.Body)
            {
                var url = "/ui/api/diagram/dashboard";

                var dashboardName = context.OwinContext.Request.Query["name"];
                if (string.IsNullOrEmpty(dashboardName))
                    url += "?r=0";
                else
                    url += "?name=" + Uri.EscapeDataString(dashboardName) + "&r=0";


                context.Html.WriteUnclosedElement(
                    "img", 
                    "id", "dashboard_diagram",
                    "class", "diagram", 
                    "src", url,
                    "onload", "reloadDashboardDiagram(this);");
            }

            return base.WritePageArea(context, pageArea);
        }
    }
}