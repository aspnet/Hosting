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
        private static IServiceCollection Import(IServiceProvider fallbackProvider, IServiceCollection additionalHostServices)
        {
            var services = additionalHostServices ?? new ServiceCollection();
            var manifest = fallbackProvider.GetRequiredService<IServiceManifest>();
            foreach (var service in manifest.Services)
            {
                services.AddTransient(service, sp => fallbackProvider.GetService(service));
            }
            return services;
        }

        // REVIEW: should additionalHostServices be an IServiceProvider??
        public static IServiceCollection Create(IConfiguration configuration = null, IServiceCollection additionalHostServices = null)
        {
            return Create(CallContextServiceLocator.Locator.ServiceProvider, configuration, additionalHostServices);
        }

        public static IServiceCollection Create(IServiceProvider fallbackServices, IConfiguration configuration = null, IServiceCollection additionalHostServices = null)
        {
            configuration = configuration ?? new Configuration();
            var services = Import(fallbackServices, additionalHostServices);
            services.AddHosting(configuration);
            services.AddSingleton<IServiceManifest>(sp => new HostingManifest(fallbackServices, additionalHostServices));
            return services;
        }

        // Manifest exposes the fallback manifest in addition to ITypeActivator, IHostingEnvironment, and ILoggerFactory
        private class HostingManifest : IServiceManifest
        {
            public HostingManifest(IServiceProvider fallback, IServiceCollection additionalHostServices = null)
            {
                var manifest = fallback.GetRequiredService<IServiceManifest>();
                Services = new Type[] {
                    typeof(ITypeActivator),
                    typeof(IHostingEnvironment),
                    typeof(ILoggerFactory),
                    typeof(IHttpContextAccessor),
                    typeof(IApplicationLifetime)
                }.Concat(manifest.Services);
                if (additionalHostServices != null)
                {
                    Services = Services.Concat(additionalHostServices.Select(s => s.ServiceType));
                }
                Services = Services.Distinct();
            }

            public IEnumerable<Type> Services { get; private set; }
        }
    }
}