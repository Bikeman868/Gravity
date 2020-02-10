namespace Gravity.Server.Interfaces
{
    public interface IExpressionParser
    {
        IExpression<T> Parse<T>(string expression);
    }
}