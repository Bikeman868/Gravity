using OwinFramework.Pages.Core.Attributes;
using OwinFramework.Pages.Core.Enums;
using OwinFramework.Pages.Core.Interfaces.Builder;
using OwinFramework.Pages.Core.Interfaces.Runtime;
using OwinFramework.Pages.Html.Elements;

namespace Gravity.Server.Ui.Components
{
    [IsComponent("node_diagram")]
    [DeployFunction(
        "void",
        "reloadNodeDiagram",
        "e",
        @"setTimeout(function() { e.src = e.src.replace(/\?r=.+/, '?r=' + Math.random()); }, 2000);")]
    internal class NodeDiagramComponent: Component
    {
        public NodeDiagramComponent(
            IComponentDependenciesFactory dependencies) 
            : base(dependencies)
        {
        }

        public override IWriteResult WritePageArea(IRenderContext context, PageArea pageArea)
        {
            if (pageArea == PageArea.Body)
            {
                var nodeName = context.OwinContext.Request.Query["name"];

                context.Html.WriteUnclosedElement(
                    "img", 
                    "id", "node_diagram",
                    "class", "diagram",
                    "src", "/ui/api/diagram/node/" + nodeName + "?r=0",
                    "onload", "reloadNodeDiagram(this);");
            }

            return base.WritePageArea(context, pageArea);
        }
    }
}