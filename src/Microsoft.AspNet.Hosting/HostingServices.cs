// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.ServiceLookup;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingServices
    {
        public static IEnumerable<IServiceDescriptor> GetDefaultServices()
        {
            return GetDefaultServices(new Configuration());
        }

        public static IEnumerable<IServiceDescriptor> GetDefaultServices(IConfiguration configuration)
        {
            var describer = new ServiceDescriber(configuration);

            yield return describer.Transient<IHostingEngine, HostingEngine>();
            yield return describer.Transient<IServerManager, ServerManager>();

            yield return describer.Transient<IStartupManager, StartupManager>();
            yield return describer.Transient<IStartupLoaderProvider, StartupLoaderProvider>();

            yield return describer.Transient<IApplicationBuilderFactory, ApplicationBuilderFactory>();
            yield return describer.Transient<IHttpContextFactory, HttpContextFactory>();

            yield return describer.Singleton<ITypeActivator, TypeActivator>();

            yield return describer.Instance<IApplicationLifetime>(new ApplicationLifetime());

            // TODO: Remove the below services and push the responsibility to frameworks to add

            // TODO: Do we expect this to be provide by the runtime eventually?
            yield return describer.Singleton<ILoggerFactory, LoggerFactory>();

            yield return describer.Scoped(typeof(IContextAccessor<>), typeof(ContextAccessor<>));

            foreach (var service in OptionsServices.GetDefaultServices())
            {
                yield return service;
            }
        }

        public static IServiceManifest BuildManifest(IServiceProvider fallback = null)
        {
            // This is the default set of services hosting exposes 
            return BuildManifest(new Type[] { typeof(ITypeActivator), typeof(IHostingEnvironment), typeof(ILoggerFactory) }, fallback);
        }

        private static IServiceManifest BuildManifest(IEnumerable<Type> types, IServiceProvider fallback = null)
        {
            IServiceManifest fallbackManifest = null;
            if (fallback != null)
            {
                fallbackManifest = fallback.GetService<IServiceManifest>();
            }
            return new HostingManifest(types, fallbackManifest);
        }

        private class HostingManifest : IServiceManifest
        {

            public HostingManifest(IEnumerable<Type> services, IServiceManifest fallback = null)
            {
                Services = services;
                if (fallback != null)
                {
                    Services = Services.Concat(fallback.Services).Distinct();
                }
            }

            public IEnumerable<Type> Services { get; private set; }
        }

    }
}
