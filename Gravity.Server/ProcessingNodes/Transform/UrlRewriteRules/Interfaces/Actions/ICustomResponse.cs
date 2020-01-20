using System.Xml.Linq;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions
{
    internal interface ICustomResponse: IAction
    {
        ICustomResponse Initialize(XElement configuration, bool stopProcessing = true, bool endRequest = true);
    }
}
