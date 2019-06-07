using Gravity.Server.Ui.Shapes;
using Svg;

namespace Gravity.Server.Interfaces
{
    internal interface IDrawingGenerator
    {
        DrawingElement GenerateDashboardDrawing();
        SvgDocument ProduceSvg(DrawingElement rootElement);
    }
}