using Svg;

namespace Gravity.Server.Ui.Shapes
{
    /// <summary>
    /// Reserves some space without drawing anything
    /// </summary>
    internal class SpacerDrawing : DrawingElement
    {
        public SpacerDrawing(int width, int height)
        {
            Width = width;
            Height = height;
            FixedSize = true;
        }

        protected override SvgElement GetContainer()
        {
            return null;
        }
    }
}