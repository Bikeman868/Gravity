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
        /// Returns true if this node has a valid path to deliver requests
        /// </summary>
        bool Available { get; }

        /// <summary>
        /// Queries the downstream and updates the Available flag
        /// </summary>
        void UpdateAvailability();

        void Bind(INodeGraph nodeGraph);
        Task ProcessRequest(IOwinContext context);
    }
}