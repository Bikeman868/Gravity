using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin;

namespace Gravity.Server.Interfaces
{
    internal interface INode
    {
        string Name { get; set; }
        bool Disabled { get; set; }

        void Bind(INodeGraph nodeGraph);
        Task ProcessRequest(IOwinContext context);
    }
}