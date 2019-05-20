using System.Xml.Linq;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations
{
    public interface IRewriteMapOperation: IOperation
    {
        string Name { get; }
        IRewriteMapOperation Initialize(XElement element);
    }
}
