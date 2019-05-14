using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Gravity.Server.Configuration;

namespace Gravity.Server.Interfaces
{
    internal interface INodeGraph
    {
        void Configure(NodeGraphConfiguration configuration);
        INode NodeByName(string name);
    }
}