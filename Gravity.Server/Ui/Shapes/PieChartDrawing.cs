using Gravity.Server.Ui.Drawings;
using Svg;
using Svg.Transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gravity.Server.Ui.Shapes
{
    internal class PieChartDrawing : RectangleDrawing
    {
        public PieChartDrawing(
            float width,
            string title,
            string units,
            Tuple<string, float>[] data,
            string numberFormat = "n1")
        {
            CssClass = "piechart";

            LeftMargin = width / 10f;
            RightMargin = width / 10f;
            TopMargin = width / 20f;
            BottomMargin = width / 20f;

            AddChild(new TextDrawing
            {
                Text = new[] { title }
            }
            .HeadingLevel(2));

            var pie = new PieDrawing(width, data);

            var sum = data.Aggregate(0f, (s, v) => s + v.Item2);

            pie.AddChild(new TextDrawing
            {
                TextSize = 2f,
                Text = new[] { sum.ToString(numberFormat), units }
            });

            AddChild(pie);

            AddChild(new LegendDrawing(units, numberFormat, data));
        }

        protected override void ArrangeChildren()
        {
            ArrangeChildrenVerticallyCentered(15f);
        }
    }
}