// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class WebHost : IWebHost
    {
        private static readonly string DeprecatedServerUrlsKey = "server.urls";

        private readonly IServiceCollection _applicationServiceCollection;
        private IStartup _startup;

        private readonly IServiceProvider _hostingServiceProvider;
        private readonly ApplicationLifetime _applicationLifetime;
        private readonly WebHostOptions _options;
        private readonly IConfiguration _config;

        private IServiceProvider _applicationServices;
        private RequestDelegate _application;
        private ILogger<WebHost> _logger;

        // Used for testing only
        internal WebHostOptions Options => _options;

        private IServer Server { get; set; }

        public WebHost(
            IServiceCollection appServices,
            IServiceProvider hostingServiceProvider,
            WebHostOptions options,
            IConfiguration config)
        {
            if (appServices == null)
            {
                throw new ArgumentNullException(nameof(appServices));
            }

            if (hostingServiceProvider == null)
            {
                throw new ArgumentNullException(nameof(hostingServiceProvider));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
            _options = options;
            _applicationServiceCollection = appServices;
            _hostingServiceProvider = hostingServiceProvider;
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
                _applicationServices = _startup.ConfigureServices(_applicationServiceCollection);
            }
        }

        private void EnsureStartup()
        {
            if (_startup != null)
            {
                return;
            }

            _startup = _hostingServiceProvider.GetRequiredService<IStartup>();
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
                Action<IApplicationBuilder> configure = _startup.Configure;
                foreach (var filter in startupFilters.Reverse())
                {
                    configure = filter.Configure(configure);
                }

                configure(builder);

                return builder.Build();
            }
            catch (Exception ex)
            {
                // EnsureApplicationServices may have failed due to a missing or throwing Startup class.
                if (_applicationServices == null)
                {
                    _applicationServices = _applicationServiceCollection.BuildServiceProvider();
                }

                // Write errors to standard out so they can be retrieved when not in development mode.
                Console.Out.WriteLine("Application startup exception: " + ex.ToString());
                var logger = _applicationServices.GetRequiredService<ILogger<WebHost>>();
                logger.ApplicationError(ex);

                if (!_options.CaptureStartupErrors)
                {
                    throw;
                }

                EnsureServer();

                // Generate an HTML error page.
                var hostingEnv = _applicationServices.GetRequiredService<IHostingEnvironment>();
                var showDetailedErrors = hostingEnv.IsDevelopment() || _options.DetailedErrors;
                var errorBytes = StartupExceptionPage.GenerateErrorHtml(showDetailedErrors, ex);

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
                    var urls = _config[WebHostDefaults.ServerUrlsKey] ?? _config[DeprecatedServerUrlsKey];
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
