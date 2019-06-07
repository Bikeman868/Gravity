using OwinFramework.Pages.Core.Attributes;
using OwinFramework.Pages.Core.Enums;
using OwinFramework.Pages.Core.Interfaces.Builder;
using OwinFramework.Pages.Core.Interfaces.Runtime;
using OwinFramework.Pages.Html.Elements;

namespace Gravity.Server.Ui.Components
{
    [IsComponent("dashboard_diagram")]
    //[NeedsComponent("api")]
    [DeployFunction("void", "updateImage", "id", "var image=getElementById(id);image.src=image.src;")]
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
                context.Html.WriteUnclosedElement(
                    "img",
                    "id", "dashboard_diagram",
                    "src", "/ui/api/diagram/dashboard",
                    "onload", "updateImage(\"dashboard_diagram\");");
            }

            return base.WritePageArea(context, pageArea);
        }
    }
}