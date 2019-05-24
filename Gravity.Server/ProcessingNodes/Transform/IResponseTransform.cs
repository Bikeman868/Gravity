using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform
{
    internal interface IResponseTransform
    {
        IOwinContext WrapOriginalRequest(IOwinContext originalContext);
        bool Transform(IOwinContext originalContext, IOwinContext wrappedContext);
    }
}