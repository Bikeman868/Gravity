using Gravity.Server.Pipeline;
using System;

namespace Gravity.Server.Interfaces
{
    internal interface IExpression<T>
    {
        Type BaseType { get; }
        T Evaluate(IRequestContext context);
    }
}