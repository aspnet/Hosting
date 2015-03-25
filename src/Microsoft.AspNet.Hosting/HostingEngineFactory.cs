// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingEngineFactory
    {
        public static IHostingEngine Create(IServiceProvider fallbackServices)
        {
            return Create(fallbackServices, configureHostingServices: null);
        }

        public static IHostingEngine Create(IServiceProvider fallbackServices, Action<IServiceCollection> configureHostingServices)
        {
            fallbackServices = fallbackServices ?? CallContextServiceLocator.Locator.ServiceProvider; // Switch to assume not null?
            var services = Import(fallbackServices, configureHostingServices);
            services.TryAdd(ServiceDescriptor.Transient<IHostingEngine, HostingEngine>());
            services.TryAdd(ServiceDescriptor.Transient<IStartupLoader, StartupLoader>());
            var hostingServices = services.BuildServiceProvider();
            return hostingServices.GetRequiredService<IHostingEngine>().UseFallbackServices(fallbackServices);
        }

        internal static IServiceCollection Import(IServiceProvider fallbackServices, Action<IServiceCollection> configureServices)
        {
            var services = new ServiceCollection();
            // Import services
            var manifest = fallbackServices.GetRequiredService<IServiceManifest>();
            foreach (var service in manifest.Services)
            {
                services.AddTransient(service, sp => fallbackServices.GetService(service));
            }

            if (configureServices != null)
            {
                configureServices(services);
            }

            return services;
        }
    }
}