using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Gravity.Server.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Actions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Conditions;
using Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules.Interfaces.Operations;
using Gravity.Server.Utility;

namespace Gravity.Server.ProcessingNodes.Transform.UrlRewriteRules
{
    internal class CustomTypeRegistrar : ICustomTypeRegistrar
    {
        private readonly IFactory _factory;
        private readonly IDictionary<string, Type> _conditions;
        private readonly IDictionary<string, Type> _operations;
        private readonly IDictionary<string, Type> _actions;

        public CustomTypeRegistrar(IFactory factory)
        {
            _factory = factory;
            _conditions = new DefaultDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            _operations = new DefaultDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            _actions = new DefaultDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        }

        public void RegisterOperation(Type type, string name)
        {
            _operations[name] = type;
        }

        public void RegisterAction(Type type, string name)
        {
            _actions[name] = type;
        }

        public void RegisterCondition(Type type, string name)
        {
            _conditions[name] = type;
        }

        public IOperation ConstructOperation(string name)
        {
            Type type;

            if (_operations.TryGetValue(name, out type))
                return _factory.Create(type) as IOperation;
            return null;
        }

        public IAction ConstructAction(string name, XElement configuration)
        {
            Type type;

            if (_actions.TryGetValue(name, out type))
            {
                var action = _factory.Create(type) as IAction;
                action?.Initialize(configuration);
                return action;
            }
            return null;
        }

        public ICondition ConstructCondition(string name, XElement configuration, IValueGetter valueGetter)
        {
            Type type;

            if (_conditions.TryGetValue(name, out type))
            {
                var condition = _factory.Create(type) as ICondition;
                condition?.Initialize(configuration, valueGetter);
                return condition;
            }
            return null;
        }
    }
}
