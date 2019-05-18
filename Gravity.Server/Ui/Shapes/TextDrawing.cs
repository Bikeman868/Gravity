using System.Linq;
using Svg;
using Svg.Transforms;

namespace Gravity.Server.Ui.Shapes
{
    internal class TextDrawing: DrawingElement
    {
        public float TextSize = 1f;
        public string[] Text;

        public override void ArrangeMargins()
        {
            base.ArrangeMargins();

            if (Text.Length > 0)
            {
                var minimumHeight = DiagramComponent.SvgTextLineSpacing * Text.Length * TextSize + TopMargin + BottomMargin;
                var minimumWidth = Text.Max(t => t == null ? 0 : t.Length) * DiagramComponent.SvgTextCharacterSpacing * TextSize + LeftMargin + RightMargin;

                if (Height < minimumHeight) Height = minimumHeight;
                if (Width < minimumWidth) Width = minimumWidth;
            }
        }

        public TextDrawing HeadingLevel(int level)
        {
            if (level == 1) TextSize = 20f / 12f;
            else if (level == 2) TextSize = 16f / 12f;
            else if (level == 3) TextSize = 13f / 12f;
            else TextSize = 1f;

            CssClass = "h" + level;

            return this;
        }

        public override SvgElement Draw()
        {
            var container = base.Draw();

            for (var lineNumber = 0; lineNumber < Text.Length; lineNumber++)
            {
                var text = new SvgText(Text[lineNumber]);
                text.Transforms.Add(new SvgTranslate(
                    LeftMargin, 
                    TopMargin + DiagramComponent.SvgTextHeight * TextSize + DiagramComponent.SvgTextLineSpacing * lineNumber * TextSize));
                text.Children.Add(new SvgTextSpan());
                container.Children.Add(text);
            }

            return container;
        }
    }
}