using Gravity.Server.Pipeline;

namespace Gravity.Server.Interfaces
{
    internal interface IExpression<T>
    {
        T Evaluate(IRequestContext context);
    }
}