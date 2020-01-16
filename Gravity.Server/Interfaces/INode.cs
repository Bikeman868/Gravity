using System;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Gravity.Server.Interfaces
{
    internal interface INode: IDisposable
    {
        /// <summary>
        /// Allows nodes to be named
        /// </summary>
        string Name { get; set; }

        /// <summary>
        ///  Allows nodes to be be disabled from the UI
        /// </summary>
        bool Disabled { get; set; }

        /// <summary>
        /// Returns true if this node can not process requests
        /// </summary>
        bool Offline { get; }

        /// <summary>
        /// Called frequently in a background thread so that nodes
        /// can maintain their status correctly without having to
        /// update on every request they process
        /// </summary>
        void UpdateStatus();

        /// <summary>
        /// This method is called after all nodes are consturcted
        /// and added to the node graph. This is an oportunity
        /// for nodes to get references to other nodes in the graph
        /// </summary>
        void Bind(INodeGraph nodeGraph);

        /// <summary>
        /// Runtime request processing
        /// </summary>
        Task ProcessRequest(IOwinContext context, ILog log);
    }
}