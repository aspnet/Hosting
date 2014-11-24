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
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingServices
    {
        private static IServiceCollection Import(IServiceProvider fallbackProvider)
        {
            var services = new ServiceCollection();
            var manifest = fallbackProvider.GetRequiredService<IServiceManifest>();
            foreach (var service in manifest.Services)
            {
                services.AddTransient(service, sp => fallbackProvider.GetService(service));
            }
            return services;
        }

        public static IServiceCollection Create(IConfiguration configuration = null)
        {
            return Create(CallContextServiceLocator.Locator.ServiceProvider, configuration);
        }

        public static IServiceCollection Create(IServiceProvider fallbackServices, IConfiguration configuration = null)
        {
            configuration = configuration ?? new Configuration();
            var services = Import(fallbackServices);
            services.AddHosting(configuration);
            services.AddSingleton<IServiceManifest>(sp => new HostingManifest(fallbackServices));
            services.AddInstance<IConfigureHostingEnvironment>(new ConfigureHostingEnvironment(configuration));
            return services;
        }

        // REVIEW: Logging doesn't depend on DI, where should this live?
        public static IServiceCollection AddLogging(this IServiceCollection services, IConfiguration config = null)
        {
            var describe = new ServiceDescriber(config ?? new Configuration());
            services.TryAdd(describe.Singleton<ILoggerFactory, LoggerFactory>());
            return services;
        }

        public static IServiceCollection AddHosting(this IServiceCollection services, IConfiguration configuration = null)
        {
            configuration = configuration ?? new Configuration();
            var describer = new ServiceDescriber(configuration);

            services.TryAdd(describer.Transient<IHostingEngine, HostingEngine>());
            services.TryAdd(describer.Transient<IServerManager, ServerManager>());

            services.TryAdd(describer.Transient<IStartupManager, StartupManager>());
            services.TryAdd(describer.Transient<IStartupLoaderProvider, StartupLoaderProvider>());

            services.TryAdd(describer.Transient<IApplicationBuilderFactory, ApplicationBuilderFactory>());
            services.TryAdd(describer.Transient<IHttpContextFactory, HttpContextFactory>());

            services.TryAdd(describer.Instance<IApplicationLifetime>(new ApplicationLifetime()));

            services.AddTypeActivator(configuration);
            // TODO: Do we expect this to be provide by the runtime eventually?
            services.AddLogging(configuration);
            // REVIEW: okay to use existing hosting environment/httpcontext if specified?
            services.TryAdd(describer.Singleton<IHostingEnvironment, HostingEnvironment>());

            // TODO: Remove this once we have IHttpContextAccessor
            services.AddContextAccessor(configuration);

            return services;
        }

        // Manifest exposes the fallback manifest in addition to ITypeActivator, IHostingEnvironment, and ILoggerFactory
        private class HostingManifest : IServiceManifest
        {
            public HostingManifest(IServiceProvider fallback)
            {
                var manifest = fallback.GetRequiredService<IServiceManifest>();
                Services = new Type[] { typeof(ITypeActivator), typeof(IHostingEnvironment), typeof(ILoggerFactory) }
                    .Concat(manifest.Services).Distinct();
            }

            public IEnumerable<Type> Services { get; private set; }
        }
    }
}