using System;
using Gravity.Server.Configuration;
using Gravity.Server.Pipeline;

namespace Gravity.Server.Interfaces
{
    internal interface INodeGraph
    {
        /// <summary>
        /// Reconfigures the node tree
        /// </summary>
        void Configure(NodeGraphConfiguration configuration);

        /// <summary>
        /// Get a node by its name
        /// </summary>
        INode NodeByName(string name);

        /// <summary>
        /// Outputs an array of nodes in a thread-safe way. Used to
        /// get information for the UI
        /// </summary>
        T[] GetNodes<T>(Func<INode, T> map, Func<INode, bool> predicate = null);

        /// <summary>
        /// If the last call to Configure resulted in an exception being thrown then
        /// it is captured here.
        /// </summary>
        Exception ConfigurationException { get; }
    }
}