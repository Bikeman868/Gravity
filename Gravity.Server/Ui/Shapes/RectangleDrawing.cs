using Svg;

namespace Gravity.Server.Ui.Shapes
{
    internal class RectangleDrawing : DrawingElement
    {
        public float CornerRadius;

        public RectangleDrawing()
        {
            LeftMargin = 5;
            RightMargin = 5;
            TopMargin = 5;
            BottomMargin = 5;

            CornerRadius = 1.5f;
        }

        protected override SvgElement GetContainer()
        {
            var container = base.GetContainer();

            var rectangle = new SvgRectangle
            {
                Height = Height,
                Width = Width,
                CornerRadiusX = CornerRadius,
                CornerRadiusY = CornerRadius
            };
            container.Children.Add(rectangle);

            return container;
        }
    }
}