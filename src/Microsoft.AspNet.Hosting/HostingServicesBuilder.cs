// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Hosting
{
    public class HostingServicesBuilder : IHostingServicesBuilder
    {
        private readonly IServiceProvider _fallbackServices;
        private readonly Action<IServiceCollection> _configureServices;
        private readonly IHostingEnvironment _hostingEnvironment;

        public HostingServicesBuilder(IServiceProvider fallbackServices, Action<IServiceCollection> configureServices)
        {
            _fallbackServices = fallbackServices ?? CallContextServiceLocator.Locator.ServiceProvider;
            _hostingEnvironment = new HostingEnvironment();
        }

        public IServiceCollection Build(bool isApplicationServices)
        {
            var services = new ServiceCollection();

            // Import from manifest

            services.AddInstance(_hostingEnvironment);
            // Add hosting engine or application services
            if (isApplicationServices)
            {
                services.AddTransient<IServerLoader, ServerLoader>();
                services.AddTransient<IApplicationBuilderFactory, ApplicationBuilderFactory>();
                services.AddTransient< IHttpContextFactory, HttpContextFactory>();
                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                services.AddLogging();

                // Conjure up a RequestServices
                services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();
            }
            else
            {
                services.AddTransient<IHostingEngineFactory, HostingEngineFactory>();
                services.AddTransient<IStartupLoader, IStartupLoader>();
            }

            if (_configureServices != null)
            {
                _configureServices(services);
            }

            return services;
        }
    }
}