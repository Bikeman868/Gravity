using System;
using OwinFramework.Builder;
using Svg;
using Svg.Transforms;

namespace Gravity.Server.Ui.Shapes
{
    internal class PopupBoxDrawing : DrawingElement
    {
        public float CornerRadius;
        private readonly string _popupId;

        public PopupBoxDrawing()
        {
            LeftMargin = 5;
            RightMargin = 5;
            TopMargin = 5;
            BottomMargin = 5;

            CornerRadius = 6f;
            ZOrder = 100;

            _popupId = Guid.NewGuid().ToShortString();
        }

        public void Attach(DrawingElement drawingElement)
        {
            var element = drawingElement.Container;
            element.CustomAttributes.Add("onmouseover", "showPopup(evt, '" + _popupId + "')");
            element.CustomAttributes.Add("onmouseout", "hidePopup(evt, '" + _popupId + "')");
        }

        protected override SvgElement GetContainer()
        {
            var container = new SvgGroup();
            container.Transforms.Add(new SvgTranslate(Left, Top));
            container.CustomAttributes.Add("visibility", "hidden");

            if (String.IsNullOrEmpty(CssClass))
                container.CustomAttributes.Add("class", "popup " + _popupId);
            else
                container.CustomAttributes.Add("class", CssClass + " popup " + _popupId);

            var rectangle = new SvgRectangle
            {
                Height = Height,
                Width = Width,
                CornerRadiusX = CornerRadius,
                CornerRadiusY = CornerRadius
            };
            container.Children.Add(rectangle);

            return container;
        }

        protected override void ArrangeChildren()
        {
            ArrangeChildrenVertically(6);
        }
    }
}