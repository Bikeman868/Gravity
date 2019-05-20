using System.Collections.Generic;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions
{
    public interface IValueConcatenator: IValueGetter
    {
        IValueGetter Initialize(IList<IValueGetter> values, string separator = null, IOperation operation = null);
        IValueGetter Initialize(IValueGetter value, IOperation operation);
    }
}
