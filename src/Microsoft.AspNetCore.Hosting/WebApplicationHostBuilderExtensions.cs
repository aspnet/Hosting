using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.Hosting
{
    public static class WebApplicationHostBuilderExtensions
    {
        public static HostBuilder UseWebApplication(this HostBuilder builder, Action<WebApplication> configure)
        {
            return builder.ConfigureServices(services =>
            {
                var diagnosticsSource = new DiagnosticListener("Microsoft.AspNet");
                services.AddSingleton<DiagnosticListener>(diagnosticsSource);
                services.AddSingleton<DiagnosticSource>(diagnosticsSource);
                services.AddSingleton<IHttpContextFactory, HttpContextFactory>();
                services.AddSingleton<IHostedService, WebService>();
                services.AddSingleton<IApplicationLifetime, ApplicationLifetime>();
                services.AddSingleton<IApplicationBuilderFactory, ApplicationBuilderFactory>();
                services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

                var app = new WebApplication(services);
                configure(app);
            });
        }
    }

    public class WebService : IHostedService, IDisposable
    {
        private readonly IServer _server;
        private readonly IStartup _startup;
        private readonly IApplicationBuilderFactory _builderFactory;
        private readonly IHttpContextFactory _contextFactory;
        private readonly DiagnosticSource _diagnosticSource;
        private readonly ILogger<WebService> _logger;
        private readonly ApplicationLifetime _lifetime;

        public WebService(IServer server, 
                          IStartup startup, 
                          IApplicationBuilderFactory builderFactory,
                          IHttpContextFactory contextFactoy,
                          DiagnosticSource diagnosticSource,
                          ILogger<WebService> logger,
                          IApplicationLifetime lifetime)
        {
            _server = server;
            _startup = startup;
            _builderFactory = builderFactory;
            _contextFactory = contextFactoy;
            _diagnosticSource = diagnosticSource;
            _logger = logger;
            _lifetime = lifetime as ApplicationLifetime;
        }

        public void Start()
        {
            var appBuilder = _builderFactory.CreateBuilder(_server.Features);
            _startup.Configure(appBuilder);
            var application = appBuilder.Build();

            var addresses = _server.Features?.Get<IServerAddressesFeature>()?.Addresses;
            if (addresses != null && !addresses.IsReadOnly && addresses.Count == 0)
            {
                if (addresses.Count == 0)
                {
                    // Provide a default address if there aren't any configured.
                    addresses.Add("http://localhost:5000");
                }
            }

            // Start the server
            _server.Start(new HostingApplication(application, _logger, _diagnosticSource, _contextFactory));

            _lifetime?.NotifyStarted();
        }

        public void Stop()
        {
            _lifetime?.StopApplication();
        }

        public void Dispose()
        {
            _lifetime?.StopApplication();

            _lifetime?.NotifyStopped();
        }
    }

    public class WebApplication
    {
        public WebApplication(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }

        public WebApplication Configure(Action<IApplicationBuilder> configureApp)
        {
            if (configureApp == null)
            {
                throw new ArgumentNullException(nameof(configureApp));
            }

            var startupAssemblyName = configureApp.GetMethodInfo().DeclaringType.GetTypeInfo().Assembly.GetName().Name;

            Services.AddSingleton<IStartup>(new DelegateStartup(configureApp));
            return this;
        }
    }
}
