// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngine : IHostingEngine
    {
        private const string EnvironmentKey = "ASPNET_ENV";

        private readonly IServiceProvider _fallbackServices;
        private readonly ApplicationLifetime _applicationLifetime;
        private readonly IApplicationEnvironment _applicationEnvironment;
        private readonly HostingEnvironment _hostingEnvironment;
        private readonly Action<IServiceCollection> _configureHostingServices;

        private Disposable _instanceStarted;

        // Start/Dispose/BuildApplicaitonServices block use methods
        // REVIEW: could split the UseServer from UseStartup
        private bool _useDisabled;

        private IServerLoader _serverLoader;
        private IApplicationBuilderFactory _builderFactory;
        private string _environmentName;
        private RequestDelegate _applicationDelegate;
        private IConfiguration _config;
        private IApplicationBuilder _builder;

        // Result of ConfigureServices and ConfigureHostingServices
        private IServiceProvider _applicationServices;


        // Only one of these should be set
        private string _startupClass;
        private StartupMethods _startup;

        // Only one of these should be set
        private string _serverFactoryLocation;
        private IServerFactory _serverFactory;
        private IServerInformation _serverInstance;

        public HostingEngine(IServiceProvider fallbackServices, IConfiguration config, Action<IServiceCollection> configureHostingServices)
        {
            _fallbackServices = fallbackServices ?? CallContextServiceLocator.Locator.ServiceProvider; // Switch to assume not null?
            _config = config ?? new Configuration();
            _configureHostingServices = configureHostingServices;
            _applicationLifetime = new ApplicationLifetime();
            _applicationEnvironment = _fallbackServices.GetRequiredService<IApplicationEnvironment>(); var appEnv = _fallbackServices.GetRequiredService<IApplicationEnvironment>();
            _hostingEnvironment = new HostingEnvironment(appEnv);
            _fallbackServices = new WrappingServiceProvider(_fallbackServices, _hostingEnvironment, _applicationLifetime);
        }

        public IDisposable Start()
        {
            EnsureApplicationServices();
            EnsureBuilder();
            EnsureServerFactory();
            InitalizeServerFactory();
            EnsureApplicationDelegate();

            var _contextFactory = _applicationServices.GetRequiredService<IHttpContextFactory>();
            var _contextAccessor = _applicationServices.GetRequiredService<IHttpContextAccessor>();
            var server = _serverFactory.Start(_serverInstance,
                features =>
                {
                    var httpContext = _contextFactory.CreateHttpContext(features);
                    _contextAccessor.HttpContext = httpContext;
                    return _applicationDelegate(httpContext);
                });

            _instanceStarted = new Disposable(() =>
            {
                _applicationLifetime.NotifyStopping();
                server.Dispose();
                _applicationLifetime.NotifyStopped();
            });

            return _instanceStarted;
        }

        private void EnsureContextDefaults()
        {
            if (_startupClass == null)
            {
                _startupClass = _applicationEnvironment.ApplicationName;
            }

            _environmentName = _config?.Get(EnvironmentKey) ?? HostingEnvironment.DefaultEnvironmentName;
            _hostingEnvironment.EnvironmentName = _environmentName;
        }

        private void EnsureApplicationServices()
        {
            _useDisabled = true;
            EnsureContextDefaults();
            EnsureStartup();

            _applicationServices = _startup.ConfigureServicesDelegate(CreateHostingServices());
        }

        private void EnsureStartup()
        {
            if (_startup != null)
            {
                return;
            }

            var diagnosticMessages = new List<string>();
            _startup = ApplicationStartup.LoadStartupMethods(
                _fallbackServices,
                _startupClass,
                _environmentName,
                diagnosticMessages);

            if (_startup == null)
            {
                throw new ArgumentException(
                    diagnosticMessages.Aggregate("Failed to find an entry point for the web application.", (a, b) => a + "\r\n" + b),
                    _startupClass);
            }
        }

        private void EnsureBuilder()
        {
            if (_builderFactory == null)
            {
                _builderFactory = _applicationServices.GetRequiredService<IApplicationBuilderFactory>();
            }

            _builder = _builderFactory.CreateBuilder();
            _builder.ApplicationServices = _applicationServices;
        }

        private void EnsureServerFactory()
        {
            if (_serverFactory != null)
            {
                return;
            }

            if (_serverLoader == null)
            {
                _serverLoader = _applicationServices.GetRequiredService<IServerLoader>();
            }

            _serverFactory = _serverLoader.LoadServerFactory(_serverFactoryLocation);
        }

        private void InitalizeServerFactory()
        {
            if (_serverInstance == null)
            {
                _serverInstance = _serverFactory.Initialize(_config);
            }

            if (_builder.Server == null)
            {
                _builder.Server = _serverInstance;
            }
        }

        private IServiceCollection CreateHostingServices()
        {
            var services = Import(_fallbackServices);

            services.TryAdd(ServiceDescriptor.Transient<IServerLoader, ServerLoader>());

            services.TryAdd(ServiceDescriptor.Transient<IApplicationBuilderFactory, ApplicationBuilderFactory>());
            services.TryAdd(ServiceDescriptor.Transient<IHttpContextFactory, HttpContextFactory>());

            // TODO: Do we expect this to be provide by the runtime eventually?
            services.AddLogging();
            services.TryAdd(ServiceDescriptor.Singleton<IHttpContextAccessor, HttpContextAccessor>());

            // Apply user hosting services
            if (_configureHostingServices != null)
            {
                _configureHostingServices(services);
            }

            // Jamming in app lifetime, app env, and hosting env since these must not be replaceable
            services.AddInstance<IApplicationLifetime>(_applicationLifetime);
            services.AddInstance<IHostingEnvironment>(_hostingEnvironment);
            services.AddInstance(_applicationEnvironment);

            // Conjure up a RequestServices
            services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();

            return services;
        }

        private void EnsureApplicationDelegate()
        {
            var startupFilters = _applicationServices.GetService<IEnumerable<IStartupFilter>>();
            var configure = _startup.ConfigureDelegate;
            foreach (var filter in startupFilters)
            {
                configure = filter.Configure(_builder, configure);
            }

            configure(_builder);

            _applicationDelegate = _builder.Build();
        }

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

        public void Dispose()
        {
            // REVIEW: this prob broke thread safety that the old using had
            if (_instanceStarted != null)
            {
                _instanceStarted.Dispose();
                _instanceStarted = null;
            }
        }

        public IServiceProvider ApplicationServices
        {
            get
            {
                EnsureApplicationServices();
                return _applicationServices;
            }
        }

        private void CheckUseAllowed()
        {
            if (_useDisabled)
            {
                throw new InvalidOperationException("HostingEngine has already been started.");
            }
        }

        public IHostingEngine UseServer(string assemblyName)
        {
            CheckUseAllowed();
            _serverFactoryLocation = assemblyName;
            return this;
        }

        public IHostingEngine UseServer(IServerFactory factory)
        {
            CheckUseAllowed();
            _serverFactory = factory;
            return this;
        }

        public IHostingEngine UseStartup(string startupClass)
        {
            CheckUseAllowed();
            _startupClass = startupClass;
            return this;
        }

        public IHostingEngine UseStartup(Action<IApplicationBuilder> configureApp, ConfigureServicesDelegate configureServices)
        {
            CheckUseAllowed();
            _startup = new StartupMethods(configureApp, configureServices);
            return this;
        }

        public IHostingEngine UseStartup(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
            CheckUseAllowed();
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

        private class WrappingServiceProvider : IServiceProvider
        {
            private readonly IServiceProvider _sp;
            private readonly IHostingEnvironment _hostingEnvironment;
            private readonly IApplicationLifetime _applicationLifetime;

            public WrappingServiceProvider(IServiceProvider sp,
                                           IHostingEnvironment hostingEnvironment,
                                           IApplicationLifetime applicationLifetime)
            {
                _sp = sp;
                _hostingEnvironment = hostingEnvironment;
                _applicationLifetime = applicationLifetime;
            }

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IHostingEnvironment))
                {
                    return _hostingEnvironment;
                }

                if (serviceType == typeof(IApplicationLifetime))
                {
                    return _applicationLifetime;
                }

                return _sp.GetService(serviceType);
            }
        }

        private class Disposable : IDisposable
        {
            private Action _dispose;

            public Disposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _dispose, () => { }).Invoke();
            }
        }
    }
}