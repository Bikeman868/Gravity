using Svg;

namespace Gravity.Server.Ui.Shapes
{
    internal class LineDrawing : DrawingElement
    {
        protected override SvgElement GetContainer()
        {
            var line = new SvgLine
            {
                StartX = Left,
                StartY = Top,
                EndX = Left + Width,
                EndY = Top + Height
            };

            if (!string.IsNullOrEmpty(CssClass))
                line.CustomAttributes.Add("class", CssClass);

            return line;
        }
    }
}