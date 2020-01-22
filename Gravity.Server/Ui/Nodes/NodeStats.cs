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

        protected DrawingElement AddSection()
        {
            var section = new SectionDrawing();
            AddChild(section);
            return section;
        }

        protected DrawingElement CreatePieChart(
            string title,
            string units,
            Tuple<string, float>[] pieChartData,
            TotalHandling totalHandling)
        {
            return new PieChartDrawing(150, title, units, pieChartData, totalHandling);
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
            var nodeConfig = FindNodeConfiguration(config, nodeName);
            return nodeConfig == null ? nodeName + " Node" : nodeName + " - " + nodeConfig.Title;
        }

        private class SectionDrawing: DrawingElement
        {
            protected override void ArrangeChildren()
            {
                ArrangeChildrenHorizontally(30f);
            }
        }
    }
}