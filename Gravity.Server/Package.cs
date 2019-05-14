﻿using System;
using System.Collections.Generic;
using Gravity.Server.DataStructures;
using Gravity.Server.Interfaces;
using Gravity.Server.ProcessingNodes;
using Ioc.Modules;
using OwinFramework.Interfaces.Builder;
using OwinFramework.Interfaces.Utility;
using OwinFramework.Pages.Core.Interfaces.Builder;
using OwinFramework.Pages.Core.Interfaces.Managers;

namespace Gravity.Server
{
    [Package]
    internal class Package: IPackage
    {
        string IPackage.Name { get { return "Gravity server"; } }
        IList<IocRegistration> IPackage.IocRegistrations { get { return _registrations; } }

        private readonly List<IocRegistration> _registrations;

        public Package()
        {
            _registrations = new List<IocRegistration>();

            _registrations.AddRange(new[]
            {
                new IocRegistration().Init<INodeGraph, NodeGraph>(),
                new IocRegistration().Init<IExpressionParser, ExpressionParser>(),
            });

            _registrations.AddRange(new[]
            {
                new IocRegistration().Init<IConfiguration>(),
                new IocRegistration().Init<IBuilder>(),
                new IocRegistration().Init<IFluentBuilder>(),
                new IocRegistration().Init<INameManager>(),
                new IocRegistration().Init<IHostingEnvironment>(),
            });
        }
    }
}