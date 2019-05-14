using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gravity.Server.Interfaces
{
    internal interface IExpressionParser
    {
        IExpression<T> Parse<T>(string expression);
    }
}