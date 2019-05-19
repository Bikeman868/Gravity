using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform
{
    internal interface IRequestTransform
    {
        void Transform(IOwinContext context);
    }
}