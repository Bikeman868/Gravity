using System;
using System.IO;
using System.Text;
using Gravity.Server.Interfaces;
using Gravity.Server.Ui.Shapes;
using OwinFramework.Pages.Core.Attributes;
using OwinFramework.Pages.Core.Enums;
using OwinFramework.Pages.Core.Interfaces.Builder;
using OwinFramework.Pages.Core.Interfaces.Runtime;
using OwinFramework.Pages.Html.Elements;

namespace Gravity.Server.Ui.Components
{
    [IsComponent("page_refresh")]
    internal class PageRefreshComponent: Component
    {
        public PageRefreshComponent(
            IComponentDependenciesFactory dependencies) 
            : base(dependencies)
        {
            PageAreas = new[] { PageArea.Head };
        }

        public override IWriteResult WritePageArea(IRenderContext context, PageArea pageArea)
        {
            if (pageArea == PageArea.Head)
            {
                context.Html.WriteUnclosedElement("meta", 
                    "http-equiv", "refresh", 
                    "content", "3");
                context.Html.WriteLine();
            }

            return base.WritePageArea(context, pageArea);
        }
    }
}
