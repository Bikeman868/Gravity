namespace Gravity.Server.Interfaces
{
    internal interface IExpressionParser
    {
        IExpression<T> Parse<T>(string expression);
    }
}