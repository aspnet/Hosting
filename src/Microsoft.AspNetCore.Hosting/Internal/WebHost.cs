// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class WebHost : IWebHost
    {
        private readonly IServiceCollection _applicationServiceCollection;
        private readonly IStartupLoader _startupLoader;
        private readonly ApplicationLifetime _applicationLifetime;
        private readonly WebHostOptions _options;
        private readonly IConfiguration _config;

        private IServiceProvider _applicationServices;
        private RequestDelegate _application;
        private ILogger<WebHost> _logger;

        // Used for testing only
        internal WebHostOptions Options => _options;

        // Only one of these should be set
        internal string StartupAssemblyName { get; set; }
        internal StartupMethods Startup { get; set; }
        internal Type StartupType { get; set; }

        private IServer Server { get; set; }

        public WebHost(
            IServiceCollection appServices,
            IStartupLoader startupLoader,
            WebHostOptions options,
            IConfiguration config)
        {
            if (appServices == null)
            {
                throw new ArgumentNullException(nameof(appServices));
            }

            if (startupLoader == null)
            {
                throw new ArgumentNullException(nameof(startupLoader));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
            _options = options;
            _applicationServiceCollection = appServices;
            _startupLoader = startupLoader;
            _applicationLifetime = new ApplicationLifetime();
            _applicationServiceCollection.AddSingleton<IApplicationLifetime>(_applicationLifetime);
        }

        public IServiceProvider Services
        {
            get
            {
                EnsureApplicationServices();
                return _applicationServices;
            }
        }

        public IFeatureCollection ServerFeatures
        {
            get
            {
                return Server?.Features;
            }
        }

        public void Initialize()
        {
            if (_application == null)
            {
                _application = BuildApplication();
            }
        }

        public virtual void Start()
        {
            Initialize();

            _logger = _applicationServices.GetRequiredService<ILogger<WebHost>>();
            var diagnosticSource = _applicationServices.GetRequiredService<DiagnosticSource>();
            var httpContextFactory = _applicationServices.GetRequiredService<IHttpContextFactory>();

            _logger.Starting();

            Server.Start(new HostingApplication(_application, _logger, diagnosticSource, httpContextFactory));

            _applicationLifetime.NotifyStarted();
            _logger.Started();
        }

        private void EnsureApplicationServices()
        {
            if (_applicationServices == null)
            {
                EnsureStartup();
                _applicationServices = Startup.ConfigureServicesDelegate(_applicationServiceCollection);
            }
        }

        private void EnsureStartup()
        {
            if (Startup != null)
            {
                return;
            }

            if (StartupType == null)
            {
                StartupType = _startupLoader.FindStartupType(StartupAssemblyName);
                if (StartupType == null)
                {
                    throw new ArgumentException("Failed to find a startup type for the web application.", StartupAssemblyName);
                }
            }

            Startup = _startupLoader.LoadMethods(StartupType);
            if (Startup == null)
            {
                throw new ArgumentException("Failed to find a startup entry point for the web application.", StartupAssemblyName);
            }
        }

        private RequestDelegate BuildApplication()
        {
            try
            {
                EnsureApplicationServices();
                EnsureServer();

                var builderFactory = _applicationServices.GetRequiredService<IApplicationBuilderFactory>();
                var builder = builderFactory.CreateBuilder(Server.Features);
                builder.ApplicationServices = _applicationServices;

                var startupFilters = _applicationServices.GetService<IEnumerable<IStartupFilter>>();
                var configure = Startup.ConfigureDelegate;
                foreach (var filter in startupFilters.Reverse())
                {
                    configure = filter.Configure(configure);
                }

                configure(builder);

                return builder.Build();
            }
            catch (Exception ex) when (_options.CaptureStartupErrors)
            {
                // EnsureApplicationServices may have failed due to a missing or throwing Startup class.
                if (_applicationServices == null)
                {
                    _applicationServices = _applicationServiceCollection.BuildServiceProvider();
                }

                EnsureServer();

                // Write errors to standard out so they can be retrieved when not in development mode.
                Console.Out.WriteLine("Application startup exception: " + ex.ToString());
                var logger = _applicationServices.GetRequiredService<ILogger<WebHost>>();
                logger.ApplicationError(ex);

                // Generate an HTML error page.
                var runtimeEnv = _applicationServices.GetRequiredService<IRuntimeEnvironment>();
                var hostingEnv = _applicationServices.GetRequiredService<IHostingEnvironment>();
                var showDetailedErrors = hostingEnv.IsDevelopment() || _options.DetailedErrors;
                var errorBytes = StartupExceptionPage.GenerateErrorHtml(showDetailedErrors, runtimeEnv, ex);

                return context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.Headers["Cache-Control"] = "private, max-age=0";
                    context.Response.ContentType = "text/html; charset=utf-8";
                    context.Response.ContentLength = errorBytes.Length;
                    return context.Response.Body.WriteAsync(errorBytes, 0, errorBytes.Length);
                };
            }
        }

        private void EnsureServer()
        {
            if (Server == null)
            {
                Server = _applicationServices.GetRequiredService<IServer>();

                var addresses = Server.Features?.Get<IServerAddressesFeature>()?.Addresses;
                if (addresses != null && !addresses.IsReadOnly && addresses.Count == 0)
                {
                    var urls = _config[WebHostDefaults.ServerUrlsKey];
                    if (!string.IsNullOrEmpty(urls))
                    {
                        foreach (var value in urls.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            addresses.Add(value);
                        }
                    }

                    if (addresses.Count == 0)
                    {
                        // Provide a default address if there aren't any configured.
                        addresses.Add("http://localhost:5000");
                    }
                }
            }
        }

        public void Dispose()
        {
            _logger?.Shutdown();
            _applicationLifetime.StopApplication();
            Server?.Dispose();
            (_applicationServices as IDisposable)?.Dispose();
            _applicationLifetime.NotifyStopped();
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
