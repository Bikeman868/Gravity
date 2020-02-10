using Gravity.Server.Ui.Shapes;
using Svg;

namespace Gravity.Server.Interfaces
{
    public interface IDrawingGenerator
    {
        DrawingElement GenerateDashboardDrawing(string dasboardName);
        DrawingElement GenerateNodeDrawing(string nodeName);
        SvgDocument ProduceSvg(DrawingElement rootElement);
    }
}