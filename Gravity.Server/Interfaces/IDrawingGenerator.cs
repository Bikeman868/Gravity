using Gravity.Server.Ui.Shapes;
using Svg;

namespace Gravity.Server.Interfaces
{
    internal interface IDrawingGenerator
    {
        DrawingElement GenerateDashboardDrawing();
        DrawingElement GenerateNodeDrawing(string nodeName);
        SvgDocument ProduceSvg(DrawingElement rootElement);
    }
}