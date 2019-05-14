using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gravity.Server.Interfaces
{
    public interface INodeGraph
    {
        INode NodeByName(string name);
    }
}