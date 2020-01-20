using System.Xml.Linq;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations
{
    internal interface IRewriteMapOperation: IOperation
    {
        string Name { get; }
        IRewriteMapOperation Initialize(XElement element);
    }
}
