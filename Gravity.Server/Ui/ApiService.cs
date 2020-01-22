using Gravity.Server.Interfaces;
using Gravity.Server.Ui.Drawings;
using OwinFramework.Pages.Core.Attributes;
using OwinFramework.Pages.Core.Enums;
using OwinFramework.Pages.Core.Interfaces.Builder;
using OwinFramework.Pages.Restful.Interfaces;
using OwinFramework.Pages.Restful.Parameters;

namespace Gravity.Server.Ui
{
    [IsService("ui", "/ui/api/", new []{ Method.Get })]
    [GenerateClientScript("api")]
    internal class ApiService: OwinFramework.Pages.Restful.Runtime.Service
    {
        private readonly IDrawingGenerator _diagramGenerator;

        public ApiService(
            IServiceDependenciesFactory serviceDependenciesFactory,
            IDrawingGenerator diagramGenerator) 
            : base(serviceDependenciesFactory)
        {
            _diagramGenerator = diagramGenerator;
        }

        [Endpoint(UrlPath = "diagram/dashboard", ResponseSerializer = typeof(SvgSerializer))]
        private void DashboardDrawing(IEndpointRequest request)
        {
            var diagram = _diagramGenerator.GenerateDashboardDrawing();
            var svg = _diagramGenerator.ProduceSvg(diagram);
            request.Success(svg);
        }

        [Endpoint(UrlPath = "diagram/node/{nodename}", ResponseSerializer = typeof(SvgSerializer))]
        [EndpointParameter("nodename", typeof(RequiredString), EndpointParameterType.PathSegment, Description = "The name of the node to return a diagram of")]
        private void NodeDrawing(IEndpointRequest request)
        {
            var nodeName = request.Parameter<string>("nodename");
            var diagram = _diagramGenerator.GenerateNodeDrawing(nodeName);
            var svg = _diagramGenerator.ProduceSvg(diagram);
            request.Success(svg);
        }
    }
}