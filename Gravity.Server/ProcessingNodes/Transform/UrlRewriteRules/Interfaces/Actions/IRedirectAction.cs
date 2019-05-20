using System.Xml.Linq;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions
{
    public interface IRedirectAction: IAction
    {
        IRedirectAction Initialize(XElement configuration, bool stopProcessing = true, bool endRequest = true);
    }
}
