using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform
{
    internal interface IResponseTransform
    {
        IOwinContext WrapOriginalRequest(IOwinContext originalContext);
        void Transform(IOwinContext originalContext, IOwinContext wrappedContext);
    }
}