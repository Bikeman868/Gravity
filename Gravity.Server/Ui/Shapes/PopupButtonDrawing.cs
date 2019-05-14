using Svg;

namespace Gravity.Server.Ui.Shapes
{
    internal class PopupButtonDrawing : ButtonDrawing
    {
        public PopupBoxDrawing PopupBox;

        public PopupButtonDrawing(DrawingElement page, string caption)
        {
            AddChild(new TextDrawing { Text = new[] { caption }, CssClass = "button" }); 
            
            PopupBox = new PopupBoxDrawing();
            page.AddChild(PopupBox);
        }

        public override void PositionPopups()
        {
            if (!ReferenceEquals(PopupBox, null))
            {
                float left, top;
                GetAbsolutePosition(out left, out top);
                PopupBox.SetAbsolutePosition(left, top + Height);
            }

            base.PositionPopups();
        }

        public override SvgElement Draw()
        {
            var drawing = base.Draw();
                
            PopupBox.Attach(this);

            return drawing;
        }
    }
}