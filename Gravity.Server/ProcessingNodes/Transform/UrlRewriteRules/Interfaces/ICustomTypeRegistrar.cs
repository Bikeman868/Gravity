using System;
using System.Xml.Linq;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces
{
    public interface ICustomTypeRegistrar
    {
        void RegisterOperation(Type type, string name);
        void RegisterAction(Type type, string name);
        void RegisterCondition(Type type, string name);

        IOperation ConstructOperation(string name);
        IAction ConstructAction(string name, XElement configuration);
        ICondition ConstructCondition(string name, XElement configuration, IValueGetter valueGetter);
    }
}
