// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Hosting
{
    public class WebHostBuilder
    {
        public const string EnvironmentKey = "ASPNET_ENV";

        private readonly IServiceProvider _fallbackServices;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILoggerFactory _loggerFactory;

        private Action<IServiceCollection> _configureServices;
        private IConfiguration _config;

        // Only one of these should be set
        private string _startupAssemblyName;
        private StartupMethods _startup;

        // Only one of these should be set
        private string _serverFactoryLocation;
        private IServerFactory _serverFactory;

        public WebHostBuilder() : this(null) { }

        public WebHostBuilder(IServiceProvider fallbackServices)
        {
            _hostingEnvironment = new HostingEnvironment();
            _loggerFactory = new LoggerFactory();
            _fallbackServices = fallbackServices ?? CallContextServiceLocator.Locator.ServiceProvider;
        }

        private IServiceCollection BuildHostingServices()
        {
            var services = new ServiceCollection();

            // Import from manifest
            var manifest = _fallbackServices.GetRequiredService<IServiceManifest>();
            foreach (var service in manifest.Services)
            {
                services.AddTransient(service, sp => _fallbackServices.GetService(service));
            }

            services.AddInstance(_hostingEnvironment);
            services.AddInstance(_loggerFactory);

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

        public IHostingEngine Build()
        {

            var hostingServices = BuildHostingServices();

            var hostingContainer = hostingServices.BuildServiceProvider();

            var appEnvironment = hostingContainer.GetRequiredService<IApplicationEnvironment>();
            var startupLoader = hostingContainer.GetRequiredService<IStartupLoader>();

            _hostingEnvironment.Initialize(appEnvironment.ApplicationBasePath, _config?[EnvironmentKey]);

            var engine = new HostingEngine(hostingServices, startupLoader, _config, _hostingEnvironment.EnvironmentName);
            if (_serverFactory != null)
            {
                engine.ServerFactory = _serverFactory;
            }
            else
            {
                engine.ServerFactoryLocation = _serverFactoryLocation;
            }

            if (_startup != null)
            {
                engine.Startup = _startup;
            }
            else
            {
                engine.StartupAssemblyName = appEnvironment.ApplicationName;
            }

            return engine;
        }

        public WebHostBuilder UseConfiguration(IConfiguration config)
        {
            _config = config;
            return this;
        }

        public WebHostBuilder UseServices(Action<IServiceCollection> configureServices)
        {
            _configureServices = configureServices;
            return this;
        }

        public WebHostBuilder UseEnvironment(string environment)
        {
            _hostingEnvironment.EnvironmentName = environment;
            return this;
        }

        public WebHostBuilder UseServer(string assemblyName)
        {
            _serverFactoryLocation = assemblyName;
            return this;
        }

        public WebHostBuilder UseServer(IServerFactory factory)
        {
            _serverFactory = factory;
            return this;
        }

        public WebHostBuilder UseStartup(string startupAssemblyName)
        {
            if (startupAssemblyName == null)
            {
                throw new ArgumentNullException(nameof(startupAssemblyName));
            }
            _startupAssemblyName = startupAssemblyName;
            return this;
        }

        public WebHostBuilder UseStartup(Action<IApplicationBuilder> configureApp)
        {
            return UseStartup(configureApp, configureServices: null);
        }

        public WebHostBuilder UseStartup(Action<IApplicationBuilder> configureApp, ConfigureServicesDelegate configureServices)
        {
            _startup = new StartupMethods(configureApp, configureServices);
            return this;
        }

        public WebHostBuilder UseStartup(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
            _startup = new StartupMethods(configureApp,
                services => {
                    if (configureServices != null)
                    {
                        configureServices(services);
                    }
                    return services.BuildServiceProvider();
                });
            return this;
        }
    }
}