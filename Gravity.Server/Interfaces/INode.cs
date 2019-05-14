using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin;

namespace Gravity.Server.Interfaces
{
    public interface INode
    {
        string Name { get; set; }
        void Bind(INodeGraph nodeGraph);
        Task ProcessRequest(IOwinContext context);
    }
}