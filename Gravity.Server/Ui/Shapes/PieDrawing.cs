using System;
using System.Linq;
using Gravity.Server.Ui.Drawings;
using Svg;
using Svg.Transforms;

namespace Gravity.Server.Ui.Shapes
{
    internal class PieDrawing: DrawingElement
    {
        private readonly Tuple<string, float>[] _data;
        private readonly string _css;

        public PieDrawing(float width, Tuple<string, float>[] data, string css)
        {
            Width = width;
            Height = width;
            FixedSize = true;
            CssClass = css + " pie";
            _data = data;
            _css = css;
        }

        public override SvgElement Draw()
        {
            var container = GetContainer();

            var sum = _data.Aggregate(0f, (s, v) => s + v.Item2);
            if (sum > 0f)
            {
                var circumference = (float)(Math.PI * Width);
                var dashOffset = circumference * 1.25f;

                for (var i = 0; i < _data.Length; i++)
                {
                    if (_data[i].Item2 > sum / 1000f)
                    {
                        var arc = _data[i].Item2 * circumference / sum;
                        var outerRing = new SvgCircle
                        {
                            CenterX = Width / 2f,
                            CenterY = Height / 2f,
                            Radius = Width * 0.45f
                        };
                        outerRing.CustomAttributes.Add("class", _css + " segment_" + ((i % 9) + 1).ToString());
                        outerRing.CustomAttributes.Add("stroke-dasharray", arc.ToString() + " " + (circumference-arc).ToString());
                        outerRing.CustomAttributes.Add("stroke-dashoffset", dashOffset.ToString());

                        container.Children.Add(outerRing);

                        dashOffset -= arc;
                    }
                }
            }

            var centerCircle = new SvgCircle
            {
                CenterX = Width / 2f,
                CenterY = Height / 2f,
                Radius = Width * 0.35f
            };
            centerCircle.CustomAttributes.Add("class", "center");
            container.Children.Add(centerCircle);

            DrawChildren(container.Children);

            return container;
        }

        protected override void ArrangeChildren()
        {
            ArrangeChildrenCentered();
        }

        public override void ArrangeMargins()
        {
        }
    }
}