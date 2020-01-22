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

        public PieDrawing(float width, Tuple<string, float>[] data)
        {
            Width = width;
            Height = width;
            FixedSize = true;
            CssClass = "pie";
            _data = data;
        }

        public override SvgElement Draw()
        {
            var container = GetContainer();
            var sum = _data.Aggregate(0f, (s, v) => s + v.Item2);

            for (var i = 0; i < _data.Length; i++)
            {
                if (_data[i].Item2 > sum / 1000f)
                {
                    var outerRing = new SvgCircle
                    {
                        CenterX = Width / 2f,
                        CenterY = Height / 2f,
                        Radius = Width * 0.45f
                    };
                    outerRing.CustomAttributes.Add("class", "piechart_outer_" + (i+1).ToString());
                    outerRing.CustomAttributes.Add("stroke-dasharray", "30 70");
                    outerRing.CustomAttributes.Add("stroke-dashoffset", (65 * i).ToString());

                    container.Children.Add(outerRing);
                }
            }

            var centerCircle = new SvgCircle
            {
                CenterX = Width / 2f,
                CenterY = Height / 2f,
                Radius = Width * 0.3f
            };
            centerCircle.CustomAttributes.Add("class", "piechart_inner");
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