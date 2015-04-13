// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Hosting
{
    public class WebHostBuilder : IHostingBuilder
    {
        public const string EnvironmentKey = "ASPNET_ENV";

        private readonly RootHostingServiceCollectionInitializer _serviceInitializer;
        private readonly IStartupLoader _startupLoader;
        private readonly IApplicationEnvironment _applicationEnvironment;
        private readonly IHostingEnvironment _hostingEnvironment;

        // Only one of these should be set
        private string _startupAssemblyName;
        private StartupMethods _startup;

        // Only one of these should be set
        private string _serverFactoryLocation;
        private IServerFactory _serverFactory;

        public WebHostBuilder(RootHostingServiceCollectionInitializer initializer, IStartupLoader startupLoader, IApplicationEnvironment appEnv, IHostingEnvironment hostingEnv)
        {
            _serviceInitializer = initializer;
            _startupLoader = startupLoader;
            _applicationEnvironment = appEnv;
            _hostingEnvironment = hostingEnv;
        }

        public IHostingEngine Build(IConfiguration config)
        {
            _hostingEnvironment.Initialize(_applicationEnvironment.ApplicationBasePath, config?[EnvironmentKey]);

            var engine = new HostingEngine(_serviceInitializer.Build(), _startupLoader, config, _hostingEnvironment, _applicationEnvironment.ApplicationName);
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
                engine.StartupAssemblyName = _applicationEnvironment.ApplicationName;
            }

            return engine;
        }

        public IHostingBuilder UseEnvironment(string environment)
        {
            _hostingEnvironment.EnvironmentName = environment;
            return this;
        }

        public IHostingBuilder UseServer(string assemblyName)
        {
            _serverFactoryLocation = assemblyName;
            return this;
        }

        public IHostingBuilder UseServer(IServerFactory factory)
        {
            _serverFactory = factory;
            return this;
        }

        public IHostingBuilder UseStartup(string startupAssemblyName)
        {
            _startupAssemblyName = startupAssemblyName;
            return this;
        }

        public IHostingBuilder UseStartup(Action<IApplicationBuilder> configureApp)
        {
            return UseStartup(configureApp, configureServices: null);
        }

        public IHostingBuilder UseStartup(Action<IApplicationBuilder> configureApp, ConfigureServicesDelegate configureServices)
        {
            _startup = new StartupMethods(configureApp, configureServices);
            return this;
        }

        public IHostingBuilder UseStartup(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
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