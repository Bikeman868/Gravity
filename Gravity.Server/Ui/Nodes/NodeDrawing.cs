using System;
using System.Collections.Generic;
using Gravity.Server.Ui.Shapes;
using OwinFramework.Pages.Core.Debug;
using OwinFramework.Pages.Core.Extensions;
using OwinFramework.Pages.Core.Interfaces;
using OwinFramework.Pages.Core.Interfaces.Runtime;
using Svg;

namespace Gravity.Server.Ui.Nodes
{
    internal class NodeDrawing : RectangleDrawing
    {
        protected readonly DrawingElement Header;
        protected readonly DrawingElement Title;
        protected SvgUnit ChildSpacing;

        public NodeDrawing(
            DrawingElement page, 
            string title, 
            int headingLevel = 2)
        {
            CornerRadius = 3f;
            ChildSpacing = 5f;

            Header = new HorizontalListDrawing();
            AddChild(Header);

            Title = new TextDrawing { Text = new[] { title } }.HeadingLevel(headingLevel);
            Header.AddChild(Title);
        }

        protected PopupBoxDrawing AddHeaderButton(DrawingElement page, string caption)
        {
            var button = new PopupButtonDrawing(page, caption);
            Header.AddChild(button);
            return button.PopupBox;
        }

        protected override void ArrangeChildren()
        {
            ArrangeChildrenVertically(ChildSpacing);
        }

        protected void AddDetails(List<string> details, DrawingElement parent)
        {
            if (details.Count > 0)
            {
                parent.AddChild(new TextDetailsDrawing { Text = details.ToArray() });
            }
        }

        protected void AddDebugInfo(List<string> text, DebugInfo debugInfo)
        {
            if (!ReferenceEquals(debugInfo.Instance, null))
                text.Add("Implemented by " + debugInfo.Instance.GetType().DisplayName());
            AddDependentComponents(text, debugInfo.DependentComponents);
            AddDataConsumer(text, debugInfo.DataConsumer);
        }

        protected void AddDependentComponents(List<string> text, List<IComponent> dependentComponents)
        {
            if (!ReferenceEquals(dependentComponents, null))
            {
                foreach (var component in dependentComponents)
                    text.Add("Depends on " + component.GetDebugInfo());
            }
        }

        protected void AddDataConsumer(List<string> text, DebugDataConsumer consumer)
        {
            if (!ReferenceEquals(consumer, null))
                text.AddRange(consumer.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None));
        }
    }
}