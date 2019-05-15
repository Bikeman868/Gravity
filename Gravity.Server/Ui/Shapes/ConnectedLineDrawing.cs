using Svg;

namespace Gravity.Server.Ui.Shapes
{
    internal class ConnectedLineDrawing : LineDrawing
    {
        public ConnectedLineDrawing(ConnectionPoint start, ConnectionPoint finish)
        {
            start.Subscribe((left, top) =>
            {
                float absLeft;
                float absTop;
                GetAbsolutePosition(out absLeft, out absTop);

                SetAbsolutePosition(left, top);

                Width = absLeft + Width - left;
                Height = absTop + Height - top;
            });

            finish.Subscribe((left, top) =>
            {
                float absLeft;
                float absTop;
                GetAbsolutePosition(out absLeft, out absTop);

                Width = left - absLeft;
                Height = top - absTop;
            });
        }
    }
}