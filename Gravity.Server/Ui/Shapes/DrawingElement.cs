using System.Collections.Generic;
using System.Linq;
using Svg;
using Svg.Transforms;

namespace Gravity.Server.Ui.Shapes
{
    internal class DrawingElement
    {
        public float Left;
        public float Top;
        public float Width;
        public float Height;

        public float LeftMargin;
        public float TopMargin;
        public float RightMargin;
        public float BottomMargin;

        public int ZOrder;
        public string CssClass;

        public bool FixedPosition;
        public bool FixedSize;

        public List<DrawingElement> Children = new List<DrawingElement>();
        public DrawingElement Parent;

        public SvgElement Container;

        protected ConnectionPoint[] ConnectionPoints;

        public void AddChild(DrawingElement element)
        {
            Children.Add(element);
            element.Parent = this;
        }

        public void UpdateConnectedElements()
        {
            foreach (var child in Children)
                child.UpdateConnectedElements();

            if (ConnectionPoints != null)
                foreach(var connectionPoint in ConnectionPoints)
                    connectionPoint.Moved();
        }

        /// <summary>
        /// Finalizes the size and position of everything inside this element
        /// </summary>
        public void Arrange()
        {
            ArrangeChildren();
            ArrangeMargins();
        }

        /// <summary>
        /// Recursively arranges all descenents of this element
        /// </summary>
        protected virtual void ArrangeChildren()
        {
            ArrangeChildrenInFixedPositions();
        }

        /// <summary>
        /// Leaves children where they were placed during drawing construction
        /// </summary>
        protected virtual void ArrangeChildrenInFixedPositions()
        {
            foreach (var child in Children)
                child.Arrange();
        }

        /// <summary>
        /// Arranges children into a horizontal row with a gap between each child
        /// </summary>
        /// <param name="gap"></param>
        protected void ArrangeChildrenHorizontally(SvgUnit gap)
        {
            var x = LeftMargin;
            var y = TopMargin;

            foreach (var child in Children)
            {
                if (!child.FixedPosition)
                {
                    child.Left = x;
                    child.Top = y;
                }

                child.Arrange();

                if (!child.FixedPosition)
                    x += child.Width + gap;
            }
        }

        /// <summary>
        /// Arranges children into a vertical column with a gap between each child
        /// </summary>
        protected void ArrangeChildrenVertically(SvgUnit gap)
        {
            var x = LeftMargin;
            var y = TopMargin;

            foreach (var child in Children)
            {
                if (!child.FixedPosition)
                {
                    child.Left = x;
                    child.Top = y;
                }

                child.Arrange();

                if (!child.FixedPosition)
                    y += child.Height + gap;
            }
        }

        /// <summary>
        /// Arranges children into a vertical column with a gap between each child
        /// and centered horizontally
        /// </summary>
        protected void ArrangeChildrenVerticallyCentered(SvgUnit gap)
        {
            ArrangeMargins();

            var x = (Width - LeftMargin - RightMargin) / 2f + LeftMargin;
            var y = TopMargin;

            foreach (var child in Children)
            {
                if (!child.FixedPosition)
                {
                    child.ArrangeMargins();
                    child.Left = x - child.Width / 2f;
                    child.Top = y;
                }

                child.Arrange();

                if (!child.FixedPosition)
                    y += child.Height + gap;
            }
        }

        /// <summary>
        /// Stacks the children centered horizontally and vertically
        /// </summary>
        protected void ArrangeChildrenCentered()
        {
            ArrangeMargins();

            var x = (Width - LeftMargin - RightMargin) / 2f + LeftMargin;
            var y = (Height - TopMargin - BottomMargin) / 2f + TopMargin;

            foreach (var child in Children)
            {
                if (!child.FixedPosition)
                {
                    child.ArrangeMargins();
                    child.Left = x - child.Width / 2f;
                    child.Top = y - child.Height / 2f;
                }

                child.Arrange();
            }
        }

        /// <summary>
        /// Moves children to create left and top margins. Sets the size of this element
        /// to create right and bottom margins
        /// </summary>
        public virtual void ArrangeMargins()
        {
            if (Children.Count == 0)
            {
                if (!FixedSize)
                {
                    Width = LeftMargin + RightMargin;
                    Height = TopMargin + BottomMargin;
                }
                return;
            }

            var moveableChildren = Children.Where(c => !c.FixedPosition).ToList();

            var minChildLeft = moveableChildren.Min(c => c.Left);
            var minChildTop = moveableChildren.Min(c => c.Top);

            var childLeftAdjustment = LeftMargin - minChildLeft;
            var childTopAdjustment = TopMargin - minChildTop;

            foreach (var child in moveableChildren)
            {
                child.Left += childLeftAdjustment;
                child.Top += childTopAdjustment;
            }

            if (!FixedSize)
            {
                Width = Children.Max(c => c.Left + c.Width) + RightMargin;
                Height = Children.Max(c => c.Top + c.Height) + BottomMargin;
            }
        }

        /// <summary>
        /// Recursively sorts all descendents by ZOrder
        /// </summary>
        public void SortDescendentsByZOrder()
        {
            foreach (var child in Children)
                child.SortDescendentsByZOrder();

            Children = 
                Children.Where(c => c.ZOrder == 0)
                .Concat(Children.Where(c => c.ZOrder > 0).OrderBy(c => c.ZOrder))
                .ToList();
        }

        /// <summary>
        /// Gets the position of this element relative to the root 
        /// element for the whole drawing
        /// </summary>
        public void GetAbsolutePosition(out float left, out float top)
        {
            left = Left;
            top = Top;

            var parent = Parent;
            while (parent != null)
            {
                left += parent.Left;
                top += parent.Top;
                parent = parent.Parent;
            }
        }

        /// <summary>
        /// Sets the position of this element relative to the root 
        /// element for the whole drawing
        /// </summary>
        public void SetAbsolutePosition(float left, float top)
        {
            float currentLeft, currentTop;
            GetAbsolutePosition(out currentLeft, out currentTop);
            Left += left - currentLeft;
            Top += top - currentTop;
        }

        /// <summary>
        /// Constructs an SVG element containing this drawing element
        /// and all of its descendents
        /// </summary>
        public virtual SvgElement Draw()
        {
            Container = GetContainer();
            if (Container != null)
                DrawChildren(Container.Children);
            return Container;
        }

        /// <summary>
        /// The children of this element will be added to the
        /// container returned by this virtual method
        /// </summary>
        protected virtual SvgElement GetContainer()
        {
            var container = new SvgGroup();
            container.Transforms.Add(new SvgTranslate(Left, Top));

            if (!string.IsNullOrEmpty(CssClass))
                container.CustomAttributes.Add("class", CssClass);

            return container;
        }

        /// <summary>
        /// Recursively draws all of the descentents within the container
        /// of this drawing element
        /// </summary>
        protected virtual void DrawChildren(SvgElementCollection container)
        {
            foreach (var child in Children)
            {
                var childDrawing = child.Draw();
                if (childDrawing != null)
                    container.Add(childDrawing);
            }
        }
    }
}