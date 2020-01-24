using System;
using System.Collections.Generic;
using Gravity.Server.Ui.Shapes;
using Svg;

namespace Gravity.Server.Ui.Nodes
{
    internal class Tile : RectangleDrawing
    {
        public ConnectionPoint TopLeftSideConnection;
        public ConnectionPoint TopRightSideConnection;
        public ConnectionPoint BottomMiddleConnection;

        protected SvgUnit ChildSpacing;

        public Tile(
            DrawingElement drawing, 
            string cssClass,
            bool disabled)
        {
            CornerRadius = 3f;
            ChildSpacing = 5f;

            CssClass = disabled ? "disabled " + cssClass : cssClass;

            TopLeftSideConnection = new ConnectionPoint(() =>
            {
                float left;
                float top;
                GetAbsolutePosition(out left, out top);
                return new Tuple<float, float>(left, top + 10);
            });

            TopRightSideConnection = new ConnectionPoint(() =>
            {
                float left;
                float top;
                GetAbsolutePosition(out left, out top);
                return new Tuple<float, float>(left + Width, top + 10);
            });

            BottomMiddleConnection = new ConnectionPoint(() =>
            {
                float left;
                float top;
                GetAbsolutePosition(out left, out top);
                return new Tuple<float, float>(left + Width / 2, top + Height);
            });

            ConnectionPoints = new[] { TopLeftSideConnection, TopRightSideConnection, BottomMiddleConnection };
        }

        protected override void ArrangeChildren()
        {
            ArrangeChildrenVertically(ChildSpacing);
        }

        protected void AddDetails(List<string> details, DrawingElement parent = null, string cssClasses = null)
        {
            if (details.Count > 0)
            {
                parent = parent ?? this;
                parent.AddChild(new TextDetailsDrawing(cssClasses)
                {
                    Text = details.ToArray()
                });
            }
        }

        public virtual void AddLines(IDictionary<string, NodeTile> nodeDrawings)
        {
        }

    }
}