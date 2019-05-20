using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces
{
    internal interface IFactory
    {
        T Create<T>();
    }
}