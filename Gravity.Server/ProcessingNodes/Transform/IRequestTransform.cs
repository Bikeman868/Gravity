using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin;

namespace Gravity.Server.ProcessingNodes.Transform
{
    internal interface IRequestTransform
    {
        void Transform(IOwinContext context);
    }
}