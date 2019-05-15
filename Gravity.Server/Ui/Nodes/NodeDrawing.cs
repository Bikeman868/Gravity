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
        protected readonly DrawingElement Label;
        protected SvgUnit ChildSpacing;

        public NodeDrawing(
            DrawingElement drawing, 
            string title, 
            int headingLevel = 2,
            string label = null)
        {
            CornerRadius = 3f;
            ChildSpacing = 5f;

            AddChild(new SpacerDrawing(15, 20));

            Header = new HorizontalListDrawing
            {
                LeftMargin = 0,
                RightMargin = 0,
                TopMargin = 0,
                BottomMargin = 0,
                Left = 0,
                Top = 0,
                FixedPosition = true
            };
            AddChild(Header);

            if (!string.IsNullOrWhiteSpace(label))
            {
                Label = new RectangleDrawing
                {
                    CssClass = "label",
                    CornerRadius = CornerRadius,
                    LeftMargin = 3,
                    TopMargin = 3,
                    BottomMargin = 2,
                    RightMargin = 2,
                    Width = 18,
                    Height = 22,
                    FixedSize = true,
                };
                Label.AddChild(new TextDrawing {Text = new[] {label}, CssClass = "label"});
                Header.AddChild(Label);
            }
            else
            {
                Header.AddChild(new SpacerDrawing(1, 1));
            }

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