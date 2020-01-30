using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gravity.Server.Interfaces;
using Microsoft.Owin;
using OwinFramework.Builder;
using OwinFramework.Interfaces.Builder;
using OwinFramework.InterfacesV1.Middleware;

namespace Gravity.Server.Middleware
{
    internal class ListenerMiddleware: IMiddleware<IRequestRewriter>
    {
        private readonly IRequestListener _requestListener;
        private readonly IList<IDependency> _dependencies = new List<IDependency>();
        IList<IDependency> IMiddleware.Dependencies { get { return _dependencies; } }

        string IMiddleware.Name { get; set; }

        public ListenerMiddleware(
            IRequestListener requestListener)
        {
            _requestListener = requestListener;
            this.RunFirst();
        }

        public Task Invoke(IOwinContext context, Func<Task> next)
        {
            return _requestListener.ProcessRequestAsync(context, next);
        }

    }
}