using Gravity.Server.Pipeline;
using System;

namespace Gravity.Server.Interfaces
{
    internal interface IExpression<T>
    {
        Type BaseType { get; }
        bool IsLiteral { get; }
        T Evaluate(IRequestContext context);
    }
}