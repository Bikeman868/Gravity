using Microsoft.Owin;

namespace Gravity.Server.Interfaces
{
    internal interface IExpression<T>
    {
        T Evaluate(IOwinContext context);
    }
}