using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Gravity.Server.Configuration;
using Microsoft.Owin;

namespace Gravity.Server.Interfaces
{
    internal interface IRequestListener
    {
        Task ProcessRequest(IOwinContext context, Func<Task> next);
        ListenerEndpointConfiguration[] Endpoints { get; }
    }
}