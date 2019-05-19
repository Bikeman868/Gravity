using System.Collections.Generic;
using System.Linq;

namespace Gravity.Server.Ui.Shapes
{
    internal class TitledListDrawing : TitledDrawing
    {
        public TitledListDrawing(string title, IEnumerable<string> list, int headingLevel = 3, string cssClasses = null)
            : base(title, headingLevel)
        {
            CssClass = "list";
            AddChild(new TextDetailsDrawing(cssClasses)
            {
                Text = list.ToArray()
            });
        }

        protected override void ArrangeChildren()
        {
            ArrangeChildrenVertically(3);
        }
    }
}