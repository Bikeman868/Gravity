using System;
using System.IO;
using System.Reflection;
using Gravity.Server;
using Gravity.Server.Middleware;
using Gravity.Server.Utility;
using Ioc.Modules;
using Microsoft.Owin;
using Ninject;
using Owin;
using OwinFramework.Builder;
using OwinFramework.Interfaces.Builder;
using OwinFramework.Interfaces.Utility;
using OwinFramework.Pages.Core;
using OwinFramework.Pages.Core.Interfaces.Builder;
using OwinFramework.Pages.Core.Interfaces.Managers;
using Urchin.Client.Sources;
using OwinFramework.Pages.DebugMiddleware;

[assembly: OwinStartup(typeof(Startup))]

namespace Gravity.Server
{
    public class Startup
    {
        private static IDisposable _configurationFileSource;

        public void Configuration(IAppBuilder app)
        {
            var packageLocator = new PackageLocator()
               .ProbeBinFolderAssemblies()
               .Add(Assembly.GetExecutingAssembly());
            var ninject = new StandardKernel(new Ioc.Modules.Ninject.Module(packageLocator));
            Factory.IocContainer = t => ninject.Get(t);

            var hostingEnvironment = ninject.Get<IHostingEnvironment>();
            var configFile = new FileInfo(hostingEnvironment.MapPath("config.json"));
            _configurationFileSource = ninject.Get<FileSource>().Initialize(configFile, TimeSpan.FromSeconds(5));

            var config = ninject.Get<IConfiguration>();

            var pipelineBuilder = ninject.Get<IBuilder>();

            pipelineBuilder.Register(ninject.Get<PagesMiddleware>()).ConfigureWith(config, "/gravity/middleware/pages");
            pipelineBuilder.Register(ninject.Get<ListenerMiddleware>()).ConfigureWith(config, "/gravity/middleware/listener");
#if DEBUG
            pipelineBuilder.Register(ninject.Get<DebugInfoMiddleware>()).ConfigureWith(config, "/gravity/middleware/debugInfo");
#endif

            app.UseBuilder(pipelineBuilder);

            var fluentBuilder = ninject.Get<IFluentBuilder>();
            ninject.Get<OwinFramework.Pages.Framework.BuildEngine>().Install(fluentBuilder);
            ninject.Get<OwinFramework.Pages.Html.BuildEngine>().Install(fluentBuilder);
            ninject.Get<OwinFramework.Pages.Restful.BuildEngine>().Install(fluentBuilder);
            fluentBuilder.Register(Assembly.GetExecutingAssembly(), t => ninject.Get(t));

            ninject.Get<INameManager>().Bind();
        }
    }
}