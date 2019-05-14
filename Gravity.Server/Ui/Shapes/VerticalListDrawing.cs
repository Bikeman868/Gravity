namespace Gravity.Server.Ui.Shapes
{
    internal class VerticalListDrawing : DrawingElement
    {
        public float ElementSpacing = 5f;

        protected override void ArrangeChildren()
        {
            ArrangeChildrenVertically(ElementSpacing);
        }
    }
}