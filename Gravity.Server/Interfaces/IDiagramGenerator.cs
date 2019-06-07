using Gravity.Server.Ui.Shapes;
using Svg;

namespace Gravity.Server.Interfaces
{
    internal interface IDiagramGenerator
    {
        DrawingElement GenerateDashboardDiagram();
        SvgDocument ProduceSvg(DrawingElement rootElement);
    }
}