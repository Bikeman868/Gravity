namespace Gravity.Server.Ui.Shapes
{
    internal class TextDetailsDrawing : TextDrawing
    {
        public TextDetailsDrawing(string cssClasses)
        {
            TextSize = 9f / 12f;

            if (string.IsNullOrEmpty(cssClasses))
                CssClass = "details";
            else
                CssClass += "details " + cssClasses;
        }
    }
}