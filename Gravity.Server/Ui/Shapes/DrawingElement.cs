using System;
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

        public List<DrawingElement> Children = new List<DrawingElement>();
        public DrawingElement Parent;

        public SvgElement Container;

        public void AddChild(DrawingElement element)
        {
            Children.Add(element);
            element.Parent = this;
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
            ArrangeChildrenStatically();
        }

        /// <summary>
        /// Leaves children where they were placed during drawing construction
        /// </summary>
        protected virtual void ArrangeChildrenStatically()
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
                child.Left = x;
                child.Top = y;
                child.Arrange();

                x += child.Width + gap;
            }
        }

        /// <summary>
        /// Arranges children into a vertical column with a gap between each child
        /// </summary>
        /// <param name="gap"></param>
        protected void ArrangeChildrenVertically(SvgUnit gap)
        {
            var x = LeftMargin;
            var y = TopMargin;

            foreach (var child in Children)
            {
                child.Left = x;
                child.Top = y;
                child.Arrange();

                y += child.Height + gap;
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
                Width = LeftMargin + RightMargin;
                Height = TopMargin + BottomMargin;
                return;
            }

            var minChildLeft = Children.Min(c => c.Left);
            var minChildTop = Children.Min(c => c.Top);

            var childLeftAdjustment = LeftMargin - minChildLeft;
            var childTopAdjustment = TopMargin - minChildTop;

            foreach (var child in Children)
            {
                child.Left += childLeftAdjustment;
                child.Top += childTopAdjustment;
            }

            Width = Children.Max(c => c.Left + c.Width) + RightMargin;
            Height = Children.Max(c => c.Top + c.Height) + BottomMargin;
        }

        /// <summary>
        /// After the whole drawing has been arranged and the position and size of
        /// everything has bee determined, this method is called to allow elements
        /// to position popup boxes to be positioned relative to the final position
        /// of the element
        /// </summary>
        public virtual void PositionPopups()
        {
            foreach (var child in Children) 
                child.PositionPopups();
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

            if (!String.IsNullOrEmpty(CssClass))
                container.CustomAttributes.Add("class", CssClass);

            return container;
        }

        /// <summary>
        /// Recursively drawss all of the descentents within the container
        /// of this drawing element
        /// </summary>
        protected virtual void DrawChildren(SvgElementCollection container)
        {
            foreach (var child in Children)
                container.Add(child.Draw());
        }
    }
}