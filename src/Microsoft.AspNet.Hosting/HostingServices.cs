// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using System;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingServices
    {

        internal class HostingManifest : IServiceManifest
        {
            // This should match GetDefaultServices, consider moving this into a dictionary so we can query based on keys
            private static readonly Type[] _services = new Type[] {
                typeof(IHostingEngine),
                typeof(IServerManager),
                typeof(IStartupManager),
                typeof(IStartupLoaderProvider),
                typeof(IApplicationBuilderFactory),
                typeof(IStartupLoaderProvider),
                typeof(IHttpContextFactory),
                typeof(ITypeActivator),
                typeof(IApplicationLifetime),
                // TODO: should remove logger?
                typeof(ILoggerFactory)
            };

            public IEnumerable<Type> Services { get { return _services; } }
        }

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
    }
}
