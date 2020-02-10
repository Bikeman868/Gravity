using Gravity.Server.Pipeline;
using System;

namespace Gravity.Server.Interfaces
{
    public interface IExpression<T>
    {
        Type BaseType { get; }
        bool IsLiteral { get; }
        T Evaluate(IRequestContext context);
        IExpression<TNew> Cast<TNew>();
    }
}