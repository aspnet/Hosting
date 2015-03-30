// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Hosting
{
    public class HostingServicesBuilder : IHostingServicesBuilder
    {
        private readonly IServiceProvider _fallbackServices;
        private readonly Action<IServiceCollection> _configureServices;

        public HostingServicesBuilder(IServiceProvider fallbackServices, Action<IServiceCollection> configureServices)
        {
            _fallbackServices = fallbackServices ?? CallContextServiceLocator.Locator.ServiceProvider;
            _configureServices = configureServices;
        }

        public IServiceCollection Build()
        {
            var services = new ServiceCollection();

            // Import from manifest
            var manifest = _fallbackServices.GetRequiredService<IServiceManifest>();
            foreach (var service in manifest.Services)
            {
                services.AddTransient(service, sp => _fallbackServices.GetService(service));
            }

            var appEnv = _fallbackServices.GetRequiredService<IApplicationEnvironment>();
            services.AddInstance<IHostingEnvironment>(new HostingEnvironment(appEnv.ApplicationBasePath));
            services.AddInstance<IHostingServicesBuilder>(this);

            services.AddTransient<IHostingFactory, HostingFactory>();
            services.AddTransient<IStartupLoader, StartupLoader>();

            services.AddTransient<IServerLoader, ServerLoader>();
            services.AddTransient<IApplicationBuilderFactory, ApplicationBuilderFactory>();
            services.AddTransient<IHttpContextFactory, HttpContextFactory>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddLogging();

            // Conjure up a RequestServices
            services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();

            if (_configureServices != null)
            {
                _configureServices(services);
            }

            return services;
        }
    }
}