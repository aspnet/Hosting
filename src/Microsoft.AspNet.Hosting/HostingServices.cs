// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.ServiceLookup;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingServices
    {
        private static IServiceCollection Import(IServiceProvider fallbackProvider, Action<IServiceCollection> configureHostServices)
        {
            var services = new ServiceCollection();
            if (configureHostServices != null)
            {
                configureHostServices(services);
            }
            var manifest = fallbackProvider.GetRequiredService<IServiceManifest>();
            foreach (var service in manifest.Services)
            {
                services.AddTransient(service, sp => fallbackProvider.GetService(service));
            }
            services.AddSingleton<IServiceManifest>(sp => new HostingManifest(services));
            return services;
        }

        public static IServiceCollection Create(IConfiguration configuration = null, Action<IServiceCollection> configureHostServices = null)
        {
            return Create(CallContextServiceLocator.Locator.ServiceProvider, configuration, configureHostServices);
        }

        public static IServiceCollection Create(IServiceProvider fallbackServices, IConfiguration configuration = null, Action<IServiceCollection> configureHostServices = null)
        {
            var services = Import(fallbackServices, configureHostServices);
            services.AddHosting(configuration);
            return services;
        }

        // Manifest exposes the fallback manifest in addition to ITypeActivator, IHostingEnvironment, and ILoggerFactory
        private class HostingManifest : IServiceManifest
        {
            public HostingManifest(IServiceCollection hostServices)
            {
                var manifest = fallback.GetRequiredService<IServiceManifest>();
                Services = new Type[] {
                    typeof(ITypeActivator),
                    typeof(IHostingEnvironment),
                    typeof(ILoggerFactory),
                    typeof(IHttpContextAccessor),
                    typeof(IApplicationLifetime)
                }.Concat(hostServices.Select(s => s.ServiceType)).Distinct();
            }

            public IEnumerable<Type> Services { get; private set; }
        }
    }
}