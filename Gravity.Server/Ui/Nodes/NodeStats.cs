using System;
using System.Linq;
using Gravity.Server.Configuration;
using Gravity.Server.Ui.Shapes;
using Svg;

namespace Gravity.Server.Ui.Nodes
{
    internal class NodeStats : DrawingElement
    {
        protected SvgUnit ChildSpacing;

        public NodeStats(DrawingElement drawing)
        {
            ChildSpacing = 30f;
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
            var pieChart = new PieChartDrawing(150, title, units, pieChartData);
            AddChild(pieChart);
        }

        protected override void ArrangeChildren()
        {
            ArrangeChildrenVertically(ChildSpacing);
        }

        protected DashboardConfiguration.NodeConfiguration FindNodeConfiguration(
            DashboardConfiguration config, string nodeName)
        {
            return config.Nodes.FirstOrDefault(n => string.Equals(n.NodeName, nodeName, StringComparison.OrdinalIgnoreCase));
        }

        protected string FindNodeTitle(
            DashboardConfiguration config, string nodeName)
        {
            return FindNodeConfiguration(config, nodeName)?.Title ?? nodeName + " Node";
        }
    }
}