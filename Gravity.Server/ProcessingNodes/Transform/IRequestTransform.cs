using Gravity.Server.Pipeline;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform
{
    /// <summary>
    /// Represents an object that can replace the content stream for
    /// the incoming or outgoing stream and transform the content as
    /// it is streamed
    /// </summary>
    internal interface IRequestTransform
    {
        /// <summary>
        /// Applies a transformation to a request 
        /// </summary>
        /// <returns>
        /// True if the request processing should be terminated
        /// because the transform generated the outgoing response
        /// </returns>
        bool Transform(IRequestContext context);
    }
}