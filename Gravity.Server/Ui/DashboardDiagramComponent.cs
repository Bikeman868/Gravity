using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Gravity.Server.Configuration;
using Gravity.Server.Ui.Nodes;
using Gravity.Server.Ui.Shapes;
using OwinFramework.Interfaces.Builder;
using OwinFramework.Pages.Core.Attributes;
using OwinFramework.Pages.Core.Debug;
using OwinFramework.Pages.Core.Enums;
using OwinFramework.Pages.Core.Interfaces.Builder;
using OwinFramework.Pages.Core.Interfaces.Runtime;
using OwinFramework.Pages.Html.Elements;
using OwinFramework.Pages.Html.Runtime;

namespace Gravity.Server.Ui
{
    [IsComponent("dashboard_diagram")]
    internal class DashboardDiagramComponent: DiagramComponent
    {
        private IDisposable _listenerConfig;
        private ListenerConfiguration _listenerConfiguration;

        public DashboardDiagramComponent(
            IComponentDependenciesFactory dependencies,
            IConfiguration configuration) 
            : base(dependencies)
        {
            _listenerConfig = configuration.Register(
                "/gravity/listener", 
                c => _listenerConfiguration = c.Sanitize(), 
                new ListenerConfiguration());
        }

        protected override DrawingElement DrawDiagram()
        {
            return new DashboardDrawing(
                _listenerConfiguration,
                null);
        }

        private class DashboardDrawing : NodeDrawing
        {
            public DashboardDrawing(
                    ListenerConfiguration listenerConfiguration,
                    NodeGraphConfiguration nodeGraphConfiguration)
                : base(null, "Dashboard", 1)
            {
                LeftMargin = 20;
                RightMargin = 20;
                TopMargin = 20;
                BottomMargin = 20;

                CssClass = "drawing";

                if (listenerConfiguration.Endpoints != null)
                {
                    foreach (var endpoint in listenerConfiguration.Endpoints)
                        AddChild(new ListenerDrawing(this, endpoint));
                }
            }

            protected override void ArrangeChildren()
            {
                ArrangeChildrenVertically(8);
            }
        }
    }
}