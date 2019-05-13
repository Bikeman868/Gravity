﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Gravity.Server;
using Ioc.Modules;
using Microsoft.Owin;
using Ninject;
using Owin;
using OwinFramework.Builder;
using OwinFramework.Interfaces.Builder;
using OwinFramework.Pages.Core;
using OwinFramework.Pages.Core.Attributes;
using OwinFramework.Pages.Core.Enums;
using OwinFramework.Pages.Core.Interfaces.Builder;
using OwinFramework.Pages.Core.Interfaces.Managers;

[assembly: OwinStartup(typeof(Startup))]

namespace Gravity.Server
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var packageLocator = new PackageLocator()
               .ProbeBinFolderAssemblies()
               .Add(Assembly.GetExecutingAssembly());
            var ninject = new StandardKernel(new Ioc.Modules.Ninject.Module(packageLocator));

            var config = ninject.Get<IConfiguration>();

            var pipelineBuilder = ninject.Get<IBuilder>();
            pipelineBuilder.Register(ninject.Get<PagesMiddleware>()).ConfigureWith(config, "/pages");

            app.UseBuilder(pipelineBuilder);

            var fluentBuilder = ninject.Get<IFluentBuilder>();
            ninject.Get<OwinFramework.Pages.Framework.BuildEngine>().Install(fluentBuilder);
            ninject.Get<OwinFramework.Pages.Html.BuildEngine>().Install(fluentBuilder);
            ninject.Get<OwinFramework.Pages.Restful.BuildEngine>().Install(fluentBuilder);
            fluentBuilder.Register(Assembly.GetExecutingAssembly());

            ninject.Get<INameManager>().Bind();
        }
    }

    [IsPage]
    [Route("/", Method.Get)]
    [PageTitle("Getting started with Owin Framework Pages")]
    [UsesLayout("homePageLayout")]
    internal class HomePage { }

    [IsLayout("homePageLayout", "region1")]
    [RegionHtml("region1", "hello-world", "Hello, world")]
    internal class HomePageLayout { }

}