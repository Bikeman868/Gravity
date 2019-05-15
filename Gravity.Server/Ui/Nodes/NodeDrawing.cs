using System;
using System.Collections.Generic;
using Gravity.Server.Ui.Shapes;
using Svg;

namespace Gravity.Server.Ui.Nodes
{
    internal class NodeDrawing : RectangleDrawing
    {
        protected readonly DrawingElement Header;
        protected readonly DrawingElement Title;
        protected SvgUnit ChildSpacing;

        public NodeDrawing(
            DrawingElement drawing, 
            string title, 
            int headingLevel = 2)
        {
            CornerRadius = 3f;
            ChildSpacing = 5f;

            Header = new HorizontalListDrawing();
            AddChild(Header);

            Title = new TextDrawing { Text = new[] { title } }.HeadingLevel(headingLevel);
            Header.AddChild(Title);
        }

        protected PopupBoxDrawing AddHeaderButton(DrawingElement page, string caption)
        {
            var button = new PopupButtonDrawing(page, caption);
            Header.AddChild(button);
            return button.PopupBox;
        }

        protected override void ArrangeChildren()
        {
            ArrangeChildrenVertically(ChildSpacing);
        }

        protected void AddDetails(List<string> details, DrawingElement parent = null)
        {
            if (details.Count > 0)
            {
                parent = parent ?? this;
                parent.AddChild(new TextDetailsDrawing { Text = details.ToArray() });
            }
        }
    }
}