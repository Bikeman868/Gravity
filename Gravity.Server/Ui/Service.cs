using Gravity.Server.Interfaces;
using OwinFramework.Pages.Core.Attributes;
using OwinFramework.Pages.Core.Enums;
using OwinFramework.Pages.Core.Interfaces.Builder;
using OwinFramework.Pages.Restful.Interfaces;

namespace Gravity.Server.Ui
{
    [IsService("ui", "/ui/api/", new []{Method.Get })]
    [GenerateClientScript("ui_api")]
    internal class Service: OwinFramework.Pages.Restful.Runtime.Service
    {
        private readonly IDiagramGenerator _diagramGenerator;

        public Service(
            IServiceDependenciesFactory serviceDependenciesFactory,
            IDiagramGenerator diagramGenerator) 
            : base(serviceDependenciesFactory)
        {
            _diagramGenerator = diagramGenerator;
        }

        [Endpoint(UrlPath = "diagram/dashboard", ResponseSerializer = typeof(SvgSerializer))]
        private void DashboardDrawing(IEndpointRequest request)
        {
            var diagram = _diagramGenerator.GenerateDashboardDiagram();
            var svg = _diagramGenerator.ProduceSvg(diagram);
            request.Success(svg);
        }
    }
}