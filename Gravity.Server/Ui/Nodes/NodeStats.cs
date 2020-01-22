using System;
using System.Collections.Generic;
using Gravity.Server.Ui.Shapes;
using Svg;

namespace Gravity.Server.Ui.Nodes
{
    internal class NodeStats : DrawingElement
    {
        protected SvgUnit ChildSpacing;

        public NodeStats(DrawingElement drawing)
        {
            ChildSpacing = 5f;
        }

        protected void AddSection(DrawingElement section)
        {
            AddChild(section);
        }

        protected void AddPieChart(
            string title,
            string units,
            Tuple<string, float>[] pieChartData)
        {
            var pieChart = new PieChartDrawing(200, title, units, pieChartData);
            AddChild(pieChart);
        }

        protected override void ArrangeChildren()
        {
            ArrangeChildrenVertically(ChildSpacing);
        }
    }
}