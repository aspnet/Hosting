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

        private HostingContext _context = new HostingContext();
        private Disposable _instanceStarted;

        // Start/Dispose/BuildApplicaitonServices block use methods
        // REVIEW: could split the UseServer from UseStartup
        private bool _useDisabled;

        private IServerLoader _serverLoader;
        private IApplicationBuilderFactory _builderFactory;
        private string _environmentName;
        private RequestDelegate _applicationDelegate;

        // Result of ConfigureServices and ConfigureHostingServices
        private IServiceProvider _applicationServices;



        public HostingEngine(IServiceProvider fallbackServices, IConfiguration config, Action<IServiceCollection> configureHostingServices)
        {
            _fallbackServices = fallbackServices ?? CallContextServiceLocator.Locator.ServiceProvider; // Switch to assume not null?
            _context.Configuration = config ?? new Configuration();
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
            var server = _context.ServerFactory.Start(_context.Server,
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
            if (_context.StartupClass == null)
            {
                _context.StartupClass = _applicationEnvironment.ApplicationName;
            }

            _environmentName = _context.Configuration?.Get(EnvironmentKey) ?? HostingEnvironment.DefaultEnvironmentName;
            _hostingEnvironment.EnvironmentName = _environmentName;
        }

        private void EnsureApplicationServices()
        {
            _useDisabled = true;
            EnsureContextDefaults();
            EnsureStartupMethods();

            _applicationServices = _context.StartupMethods.ConfigureServicesDelegate(CreateHostingServices());
        }

        private void EnsureStartupMethods()
        {
            if (_context.StartupMethods != null)
            {
                return;
            }

            var diagnosticMessages = new List<string>();
            _context.StartupMethods = ApplicationStartup.LoadStartupMethods(
                _fallbackServices,
                _context.StartupClass,
                _environmentName,
                diagnosticMessages);

            if (_context.StartupMethods == null)
            {
                throw new ArgumentException(
                    diagnosticMessages.Aggregate("Failed to find an entry point for the web application.", (a, b) => a + "\r\n" + b),
                    nameof(_context));
            }
        }

        private void EnsureBuilder()
        {
            if (_builderFactory == null)
            {
                _builderFactory = _applicationServices.GetRequiredService<IApplicationBuilderFactory>();
            }

            _context.Builder = _builderFactory.CreateBuilder();
            _context.Builder.ApplicationServices = _applicationServices;
        }

        private void EnsureServerFactory()
        {
            if (_context.ServerFactory != null)
            {
                return;
            }

            if (_serverLoader == null)
            {
                _serverLoader = _applicationServices.GetRequiredService<IServerLoader>();
            }

            _context.ServerFactory = _serverLoader.LoadServerFactory(_context.ServerFactoryLocation);
        }

        private void InitalizeServerFactory()
        {
            if (_context.Server == null)
            {
                _context.Server = _context.ServerFactory.Initialize(_context.Configuration);
            }

            if (_context.Builder.Server == null)
            {
                _context.Builder.Server = _context.Server;
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
            var configure = _context.StartupMethods.ConfigureDelegate;
            foreach (var filter in startupFilters)
            {
                configure = filter.Configure(_context.Builder, configure);
            }

            configure(_context.Builder);

            _applicationDelegate = _context.Builder.Build();
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
            _context.ServerFactoryLocation = assemblyName;
            return this;
        }

        public IHostingEngine UseServer(IServerFactory factory)
        {
            CheckUseAllowed();
            _context.ServerFactory = factory;
            return this;
        }

        public IHostingEngine UseStartup(string startupClass)
        {
            CheckUseAllowed();
            _context.StartupClass = startupClass;
            return this;
        }

        public IHostingEngine UseStartup(Action<IApplicationBuilder> configureApp, ConfigureServicesDelegate configureServices)
        {
            CheckUseAllowed();
            _context.StartupMethods = new StartupMethods(configureApp, configureServices);
            return this;
        }

        public IHostingEngine UseStartup(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
            CheckUseAllowed();
            _context.StartupMethods = new StartupMethods(configureApp, 
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

        private class HostingContext
        {
            public IConfiguration Configuration { get; set; }

            public IApplicationBuilder Builder { get; set; }

            public string StartupClass { get; set; }
            public StartupMethods StartupMethods { get; set; }

            public string ServerFactoryLocation { get; set; }
            public IServerFactory ServerFactory { get; set; }
            public IServerInformation Server { get; set; }
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