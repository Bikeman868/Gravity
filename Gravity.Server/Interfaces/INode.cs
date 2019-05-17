using System;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Gravity.Server.Interfaces
{
    internal interface INode: IDisposable
    {
        string Name { get; set; }
        bool Disabled { get; set; }

        void Bind(INodeGraph nodeGraph);
        Task ProcessRequest(IOwinContext context);
    }
}