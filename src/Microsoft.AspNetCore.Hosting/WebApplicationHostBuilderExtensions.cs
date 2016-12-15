using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.Hosting
{
    public static class WebApplicationHostBuilderExtensions
    {
        public static HostBuilder UseWebApplication(this HostBuilder builder, Action<WebApplication> configure)
        {
            if (string.IsNullOrEmpty(builder.GetSetting(WebHostDefaults.EnvironmentKey)))
            {
                // Try adding legacy environment keys, never remove these.
                builder.UseSetting(WebHostDefaults.EnvironmentKey, Environment.GetEnvironmentVariable("Hosting:Environment")
                    ?? Environment.GetEnvironmentVariable("ASPNET_ENV"));
            }

            if (string.IsNullOrEmpty(builder.GetSetting(WebHostDefaults.ServerUrlsKey)))
            {
                // Try adding legacy url key, never remove this.
                builder.UseSetting(WebHostDefaults.ServerUrlsKey, Environment.GetEnvironmentVariable("ASPNETCORE_SERVER.URLS"));
            }

            return builder.ConfigureServices(services =>
            {
                var options = new WebHostOptions(builder);

                var appEnvironment = PlatformServices.Default.Application;
                var contentRootPath = ResolveContentRootPath(options.ContentRootPath, appEnvironment.ApplicationBasePath);
                var applicationName = options.ApplicationName ?? appEnvironment.ApplicationName;

                var hostingEnvironment = new HostingEnvironment();

                // Initialize the hosting environment
                hostingEnvironment.Initialize(applicationName, contentRootPath, options);

                var diagnosticsSource = new DiagnosticListener("Microsoft.AspNet");
                services.AddSingleton<DiagnosticListener>(diagnosticsSource);
                services.AddSingleton<DiagnosticSource>(diagnosticsSource);
                services.AddSingleton<IHttpContextFactory, HttpContextFactory>();
                services.AddSingleton<IApplicationLifetime, ApplicationLifetime>();
                services.AddSingleton<IApplicationBuilderFactory, ApplicationBuilderFactory>();
                services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
                services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();
                services.AddSingleton<IHostingEnvironment>(hostingEnvironment);
                services.AddSingleton<IHostedService, WebService>();

                if (!string.IsNullOrEmpty(options.StartupAssembly))
                {
                    try
                    {
                        var startupType = StartupLoader.FindStartupType(options.StartupAssembly, hostingEnvironment.EnvironmentName);

                        if (typeof(IStartup).GetTypeInfo().IsAssignableFrom(startupType.GetTypeInfo()))
                        {
                            services.AddSingleton(typeof(IStartup), startupType);
                        }
                        else
                        {
                            services.AddSingleton(typeof(IStartup), sp =>
                            {
                                var methods = StartupLoader.LoadMethods(sp, startupType, hostingEnvironment.EnvironmentName);
                                return new ConventionBasedStartup(methods);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        var capture = ExceptionDispatchInfo.Capture(ex);
                        services.AddSingleton<IStartup>(_ =>
                        {
                            capture.Throw();
                            return null;
                        });
                    }
                }

                var app = new WebApplication(services);
                configure(app);
            });
        }

        private static string ResolveContentRootPath(string contentRootPath, string basePath)
        {
            if (string.IsNullOrEmpty(contentRootPath))
            {
                return basePath;
            }
            if (Path.IsPathRooted(contentRootPath))
            {
                return contentRootPath;
            }
            return Path.Combine(Path.GetFullPath(basePath), contentRootPath);
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
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<IStartupFilter> _filters;

        public WebService(IServer server,
                          IStartup startup,
                          IApplicationBuilderFactory builderFactory,
                          IHttpContextFactory contextFactoy,
                          DiagnosticSource diagnosticSource,
                          ILogger<WebService> logger,
                          IApplicationLifetime lifetime,
                          IServiceProvider serviceProvider,
                          IEnumerable<IStartupFilter> filters)
        {
            _server = server;
            _startup = startup;
            _builderFactory = builderFactory;
            _contextFactory = contextFactoy;
            _diagnosticSource = diagnosticSource;
            _logger = logger;
            _lifetime = lifetime as ApplicationLifetime;
            _serviceProvider = serviceProvider;
            _filters = filters;
        }

        public void Start()
        {

            var appBuilder = _builderFactory.CreateBuilder(_server.Features);
            appBuilder.ApplicationServices = _serviceProvider;

            Action<IApplicationBuilder> configure = _startup.Configure;
            foreach (var filter in _filters.Reverse())
            {
                configure = filter.Configure(configure);
            }

            configure(appBuilder);
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
