using System.Collections.Generic;
using Gravity.Server.Interfaces;
using Gravity.Server.Pipeline;
using Gravity.Server.ProcessingNodes.Routing;
using Gravity.Server.Ui.Drawings;
using Gravity.Server.Utility;
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
                new IocRegistration().Init<IRequestListener, RequestListener>(),
                new IocRegistration().Init<IFactory, Factory>(),
                new IocRegistration().Init<IDrawingGenerator, DiagramGenerator>(),
                new IocRegistration().Init<ILogFactory, LogFactory>(),
                new IocRegistration().Init<IBufferPool, BufferPool>(),
                new IocRegistration().Init<IConnectionThreadPool, ConnectionThreadPool>(),
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