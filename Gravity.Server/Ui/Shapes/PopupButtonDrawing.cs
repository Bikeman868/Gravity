using Svg;

namespace Gravity.Server.Ui.Shapes
{
    internal class PopupButtonDrawing : ButtonDrawing
    {
        public PopupBoxDrawing PopupBox;

        public PopupButtonDrawing(DrawingElement page, string caption, ConnectionPoint connectionPoint)
        {
            AddChild(new TextDrawing { Text = new[] { caption }, CssClass = "button" }); 
            
            PopupBox = new PopupBoxDrawing();
            page.AddChild(PopupBox);

            connectionPoint.Subscribe((left, top) =>
            {
                if (!ReferenceEquals(PopupBox, null))
                {
                    PopupBox.SetAbsolutePosition(left, top + Height);
                }
            });
        }

        public override SvgElement Draw()
        {
            var drawing = base.Draw();
                
            PopupBox.Attach(this);

            return drawing;
        }
    }
}