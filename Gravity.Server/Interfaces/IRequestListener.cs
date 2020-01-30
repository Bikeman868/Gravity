using System;
using System.Threading.Tasks;
using Gravity.Server.Configuration;
using Microsoft.Owin;

namespace Gravity.Server.Interfaces
{
    internal interface IRequestListener
    {
        Task ProcessRequestAsync(IOwinContext context, Func<Task> next);
        ListenerEndpointConfiguration[] Endpoints { get; }
    }
}