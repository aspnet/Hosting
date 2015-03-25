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

        private IServiceProvider _fallbackServices;
        private readonly IStartupLoader _startupLoader;
        private readonly ApplicationLifetime _applicationLifetime;
        private readonly IApplicationEnvironment _applicationEnvironment;
        private readonly HostingEnvironment _hostingEnvironment;

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
        private IServiceProvider _applicationServices;

        // Only one of these should be set
        private string _startupName;
        private Type _startupType;
        private StartupMethods _startup;

        // Only one of these should be set
        private string _serverFactoryLocation;
        private IServerFactory _serverFactory;
        private IServerInformation _serverInstance;

        public HostingEngine(IStartupLoader startupLoader, IApplicationEnvironment appEnv)
        {
            _startupLoader = startupLoader;
            _applicationEnvironment = appEnv;
            _applicationLifetime = new ApplicationLifetime();
            _hostingEnvironment = new HostingEnvironment(appEnv);
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

        private void EnsureDefaults()
        {
            if (_startupName == null)
            {
                _startupName = _applicationEnvironment.ApplicationName;
            }

            _fallbackServices = _fallbackServices ?? CallContextServiceLocator.Locator.ServiceProvider;
            _config = _config ?? new Configuration();
            _environmentName = _config?.Get(EnvironmentKey) ?? HostingEnvironment.DefaultEnvironmentName;
            _hostingEnvironment.EnvironmentName = _environmentName;
        }

        private void EnsureApplicationServices()
        {
            _useDisabled = true;
            EnsureDefaults();
            EnsureStartup();

            var fallbackServices = new WrappingServiceProvider(_fallbackServices, _hostingEnvironment, _applicationLifetime);

            var hostingServices = HostingEngineFactory.Import(fallbackServices, 
                services =>
                {
                    services.TryAdd(ServiceDescriptor.Transient<IServerLoader, ServerLoader>());
                    services.TryAdd(ServiceDescriptor.Transient<IApplicationBuilderFactory, ApplicationBuilderFactory>());
                    services.TryAdd(ServiceDescriptor.Transient<IHttpContextFactory, HttpContextFactory>());
                    services.TryAdd(ServiceDescriptor.Singleton<IHttpContextAccessor, HttpContextAccessor>());

                    // TODO: Do we expect this to be provide by the runtime eventually?
                    services.AddLogging();

                    // Jamming in app lifetime, app env, and hosting env since these must not be replaceable
                    services.AddInstance<IApplicationLifetime>(_applicationLifetime);
                    services.AddInstance<IHostingEnvironment>(_hostingEnvironment);
                    //services.AddInstance(_applicationEnvironment);

                    // Conjure up a RequestServices
                    services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();
                });

        _applicationServices = _startup.ConfigureServicesDelegate(hostingServices);
        }

        private void EnsureStartup()
        {
            if (_startup != null)
            {
                return;
            }

            var diagnosticMessages = new List<string>();
            if (_startupType != null)
            {
                _startup = _startupLoader.Load(
                    _fallbackServices,
                    _startupType,
                    _environmentName,
                    diagnosticMessages);
            }
            else
            {
                _startup = _startupLoader.Load(
                    _fallbackServices,
                    _startupName,
                    _environmentName,
                    diagnosticMessages);
            }

            if (_startup == null)
            {
                throw new ArgumentException(
                    diagnosticMessages.Aggregate("Failed to find a startup entry point for the web application.", (a, b) => a + "\r\n" + b),
                    _startupName);
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
            // REVIEW: why is instance on _builder as well? currently we have no UseServer(instance), so this is always null
            if (_serverInstance == null)
            {
                _serverInstance = _serverFactory.Initialize(_config);
            }

            if (_builder.Server == null)
            {
                _builder.Server = _serverInstance;
            }
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

        public IHostingEngine UseFallbackServices(IServiceProvider services)
        {
            CheckUseAllowed();
            _fallbackServices = services;
            return this;
        }

        public IHostingEngine UseConfiguration(IConfiguration config)
        {
            CheckUseAllowed();
            _config = config ?? new Configuration();
            return this;
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

        public IHostingEngine UseStartup(string startupName)
        {
            CheckUseAllowed();
            _startupName = startupName;
            return this;
        }

        public IHostingEngine UseStartup<T>() where T : class
        {
            CheckUseAllowed();
            _startupType = typeof(T);
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

        internal class WrappingServiceProvider : IServiceProvider
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