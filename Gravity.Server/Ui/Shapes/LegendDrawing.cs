using System;
using System.Linq;
using Gravity.Server.Ui.Drawings;
using Svg;
using Svg.Transforms;

namespace Gravity.Server.Ui.Shapes
{
    internal class LegendDrawing: DrawingElement
    {
        private readonly Tuple<string, float>[] _data;

        public LegendDrawing(string units, string numberFormat, Tuple<string, float>[] data)
        {
            CssClass = "legend";

            for (var i = 0; i < data.Length; i++)
            {
                var label = $"{data[i].Item1} ({data[i].Item2.ToString(numberFormat)}{units})";
                AddChild(new LegendEntryDrawing(i, label, 5f));
            }
        }

        protected override void ArrangeChildren()
        {
            ArrangeChildrenVertically(6f);
        }

        private class LegendEntryDrawing: DrawingElement
        {
            private int _segment;
            private string _label;
            private float _gap;

            public LegendEntryDrawing(int segment, string label, float gap)
            {
                _segment = segment;
                _label = label;
                _gap = gap;

                Height = DiagramGenerator.SvgTextHeight;
                Width = Height + gap + DiagramGenerator.SvgTextCharacterSpacing * label.Length;
                FixedSize = true;
            }

            public override SvgElement Draw()
            {
                var container = GetContainer();

                var ring = new SvgCircle
                {
                    CenterX = Height / 2f,
                    CenterY = Height / 2f,
                    Radius = Height * 0.45f
                };
                ring.CustomAttributes.Add("class", "segment_" + (_segment + 1).ToString());
                container.Children.Add(ring);

                var label = new SvgText(_label);
                label.Transforms.Add(new SvgTranslate(Height + _gap, Height));
                label.Children.Add(new SvgTextSpan());
                container.Children.Add(label);

                return container;
            }
        }
    }
}