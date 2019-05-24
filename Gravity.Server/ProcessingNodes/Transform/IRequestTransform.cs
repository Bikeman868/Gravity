using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform
{
    internal interface IRequestTransform
    {
        bool Transform(IOwinContext context);
    }
}