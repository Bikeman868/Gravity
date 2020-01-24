using System;
using System.Collections.Generic;
using Gravity.Server.Ui.Shapes;
using Svg;

namespace Gravity.Server.Ui.Nodes
{
    internal class NodeTile : Tile
    {
        protected readonly DrawingElement Header;
        protected readonly DrawingElement Title;
        protected readonly DrawingElement Label;
        protected readonly TextDrawing LabelText;

        public NodeTile(
            DrawingElement drawing, 
            string title, 
            string cssClass,
            bool disabled,
            int headingLevel = 2,
            string label = null)
            : base(drawing, cssClass, disabled)
        {
            AddChild(new SpacerDrawing(15, 20));

            Header = new HorizontalListDrawing
            {
                LeftMargin = 0,
                RightMargin = 0,
                TopMargin = 0,
                BottomMargin = 0,
                Left = 0,
                Top = 0,
                FixedPosition = true,
                CssClass = CssClass
            };
            AddChild(Header);

            if (!string.IsNullOrWhiteSpace(label))
            {
                Label = new RectangleDrawing
                {
                    CssClass = disabled ? "disabled label" : "label",
                    CornerRadius = CornerRadius,
                    LeftMargin = 3,
                    TopMargin = 3,
                    BottomMargin = 2,
                    RightMargin = 2,
                    Width = 18,
                    Height = 22,
                    FixedSize = true,
                };

                LabelText = new TextDrawing
                {
                    Text = new[] { label },
                    CssClass = disabled ? "disabled label" : "label"
                };

                if (headingLevel == 3)
                {
                    Label.Width = 14;
                    Label.Height = 16;
                    Label.TopMargin = 3;
                    Label.LeftMargin = 2;
                    LabelText.TextSize = 9f / 12f;
                }

                Label.AddChild(LabelText);
                Header.AddChild(Label);
            }
            else
            {
                Header.AddChild(new SpacerDrawing(1, 1));
            }

            Title = new TextDrawing
            {
                Text = new[] { title }
            }
            .HeadingLevel(headingLevel);

            if (disabled) Title.CssClass = "disabled " + Title.CssClass;

            Header.AddChild(Title);
        }

        protected PopupBoxDrawing AddHeaderButton(DrawingElement page, string caption)
        {
            var button = new PopupButtonDrawing(page, caption, BottomMiddleConnection);
            Header.AddChild(button);
            return button.PopupBox;
        }
    }
}