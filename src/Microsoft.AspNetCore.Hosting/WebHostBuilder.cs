// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// A builder for <see cref="IWebHost"/>
    /// </summary>
    public class WebHostBuilder : IWebHostBuilder
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly List<Action<IServiceCollection>> _configureServicesDelegates;
        private readonly List<Action<ILoggerFactory>> _configureLoggingDelegates;

        private IConfiguration _config;
        private ILoggerFactory _loggerFactory;
        private WebHostOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class.
        /// </summary>
        public WebHostBuilder()
        {
            _hostingEnvironment = new HostingEnvironment();
            _configureServicesDelegates = new List<Action<IServiceCollection>>();
            _configureLoggingDelegates = new List<Action<ILoggerFactory>>();

            _config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .Build();

            if (string.IsNullOrEmpty(GetSetting(WebHostDefaults.EnvironmentKey)))
            {
                // Try adding legacy environment keys, never remove these.
                UseSetting(WebHostDefaults.EnvironmentKey, Environment.GetEnvironmentVariable("Hosting:Environment")
                    ?? Environment.GetEnvironmentVariable("ASPNET_ENV"));
            }

            if (string.IsNullOrEmpty(GetSetting(WebHostDefaults.ServerUrlsKey)))
            {
                // Try adding legacy url key, never remove this.
                UseSetting(WebHostDefaults.ServerUrlsKey, Environment.GetEnvironmentVariable("ASPNETCORE_SERVER.URLS"));
            }
        }

        /// <summary>
        /// Add or replace a setting in the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to add or replace.</param>
        /// <param name="value">The value of the setting to add or replace.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public IWebHostBuilder UseSetting(string key, string value)
        {
            _config[key] = value;
            return this;
        }

        /// <summary>
        /// Get the setting value from the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to look up.</param>
        /// <returns>The value the setting currently contains.</returns>
        public string GetSetting(string key)
        {
            return _config[key];
        }

        /// <summary>
        /// Specify the <see cref="ILoggerFactory"/> to be used by the web host.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to be used.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public IWebHostBuilder UseLoggerFactory(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _loggerFactory = loggerFactory;
            return this;
        }

        /// <summary>
        /// Adds a delegate for configuring additional services for the host or web application. This may be called
        /// multiple times.
        /// </summary>
        /// <param name="configureServices">A delegate for configuring the <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            if (configureServices == null)
            {
                throw new ArgumentNullException(nameof(configureServices));
            }

            _configureServicesDelegates.Add(configureServices);
            return this;
        }

        /// <summary>
        /// Adds a delegate for configuring the provided <see cref="ILoggerFactory"/>. This may be called multiple times.
        /// </summary>
        /// <param name="configureLogging">The delegate that configures the <see cref="ILoggerFactory"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public IWebHostBuilder ConfigureLogging(Action<ILoggerFactory> configureLogging)
        {
            if (configureLogging == null)
            {
                throw new ArgumentNullException(nameof(configureLogging));
            }

            _configureLoggingDelegates.Add(configureLogging);
            return this;
        }

        /// <summary>
        /// Builds the required services and an <see cref="IWebHost"/> which hosts a web application.
        /// </summary>
        public IWebHost Build()
        {
            // Warn about deprecated environment variables
            if (Environment.GetEnvironmentVariable("Hosting:Environment") != null)
            {
                Console.WriteLine("The environment variable 'Hosting:Environment' is obsolete and has been replaced with 'ASPNETCORE_ENVIRONMENT'");
            }

            if (Environment.GetEnvironmentVariable("ASPNET_ENV") != null)
            {
                Console.WriteLine("The environment variable 'ASPNET_ENV' is obsolete and has been replaced with 'ASPNETCORE_ENVIRONMENT'");
            }

            if (Environment.GetEnvironmentVariable("ASPNETCORE_SERVER.URLS") != null)
            {
                Console.WriteLine("The environment variable 'ASPNETCORE_SERVER.URLS' is obsolete and has been replaced with 'ASPNETCORE_URLS'");
            }

            // This startup error will be null unless captureStartupErrors was set to true
            ExceptionDispatchInfo startupError;
            var hostingServices = BuildHostingServices(out startupError);
            var hostingContainer = hostingServices.BuildServiceProvider();

            var host = new WebHost(hostingServices, hostingContainer, _options, _config, startupError);

            host.Initialize();

            return host;
        }

        private IServiceCollection BuildHostingServices(out ExceptionDispatchInfo startupError)
        {
            startupError = null;
            _options = new WebHostOptions(_config);

            var appEnvironment = PlatformServices.Default.Application;
            var contentRootPath = ResolveContentRootPath(_options.ContentRootPath, appEnvironment.ApplicationBasePath);
            var applicationName = _options.ApplicationName ?? appEnvironment.ApplicationName;

            // Initialize the hosting environment
            _hostingEnvironment.Initialize(applicationName, contentRootPath, _options);

            var services = new ServiceCollection();
            services.AddSingleton(_hostingEnvironment);

            if (_loggerFactory == null)
            {
                _loggerFactory = new LoggerFactory();
            }

            try
            {
                // Execute the platform light-up assemblies
                foreach (var assemblyName in _options.PlatformAssemblies)
                {
                    var assembly = Assembly.Load(new AssemblyName(assemblyName));

                    foreach (var attribute in assembly.GetCustomAttributes<HostingStartupAttribute>())
                    {
                        var hostingStartup = (IHostingStartup)Activator.CreateInstance(attribute.HostingStartupType);
                        hostingStartup.Configure(this);
                    }
                }
            }
            catch (Exception ex) when (_options.CaptureStartupErrors)
            {
                // If we're capturing startup errors then delay this until the application startup logic so that we can show the user
                // a nice error page
                startupError = ExceptionDispatchInfo.Capture(ex);
            }

            foreach (var configureLogging in _configureLoggingDelegates)
            {
                configureLogging(_loggerFactory);
            }

            //The configured ILoggerFactory is added as a singleton here. AddLogging below will not add an additional one.
            services.AddSingleton(_loggerFactory);

            //This is required to add ILogger of T.
            services.AddLogging();

            services.AddTransient<IApplicationBuilderFactory, ApplicationBuilderFactory>();
            services.AddTransient<IHttpContextFactory, HttpContextFactory>();
            services.AddOptions();

            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
            services.AddSingleton<DiagnosticSource>(diagnosticSource);
            services.AddSingleton<DiagnosticListener>(diagnosticSource);

            // Conjure up a RequestServices
            services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();
            services.AddTransient<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();

            // Ensure object pooling is available everywhere.
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

            if (!string.IsNullOrEmpty(_options.StartupAssembly))
            {
                try
                {
                    var startupType = StartupLoader.FindStartupType(_options.StartupAssembly, _hostingEnvironment.EnvironmentName);

                    if (typeof(IStartup).GetTypeInfo().IsAssignableFrom(startupType.GetTypeInfo()))
                    {
                        services.AddSingleton(typeof(IStartup), startupType);
                    }
                    else
                    {
                        services.AddSingleton(typeof(IStartup), sp =>
                        {
                            var hostingEnvironment = sp.GetRequiredService<IHostingEnvironment>();
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

            foreach (var configureServices in _configureServicesDelegates)
            {
                configureServices(services);
            }

            return services;
        }

        private string ResolveContentRootPath(string contentRootPath, string basePath)
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
}
