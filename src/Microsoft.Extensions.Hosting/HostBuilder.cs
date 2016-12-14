using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting
{

    /// <summary>
    /// A builder for <see cref="HostBuilder"/>
    /// </summary>
    public class HostBuilder : IHostLifetimeControl
    {
        private readonly List<Action<IServiceCollection>> _configureServicesDelegates;
        private readonly List<Action<ILoggerFactory>> _configureLoggingDelegates;

        private IConfiguration _config;
        private ILoggerFactory _loggerFactory;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _hostBuilt;

        /// <summary>
        /// Initializes a new instance of the <see cref="HostBuilder"/> class.
        /// </summary>
        public HostBuilder()
        {
            _configureServicesDelegates = new List<Action<IServiceCollection>>();
            _configureLoggingDelegates = new List<Action<ILoggerFactory>>();

            _config = new ConfigurationBuilder()
                .Build();
        }

        /// <summary>
        /// Add or replace a setting in the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to add or replace.</param>
        /// <param name="value">The value of the setting to add or replace.</param>
        /// <returns>The <see cref="HostBuilder"/>.</returns>
        public HostBuilder UseSetting(string key, string value)
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
        /// <returns>The <see cref="HostBuilder"/>.</returns>
        public HostBuilder UseLoggerFactory(ILoggerFactory loggerFactory)
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
        /// <returns>The <see cref="HostBuilder"/>.</returns>
        public HostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
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
        /// <returns>The <see cref="HostBuilder"/>.</returns>
        public HostBuilder ConfigureLogging(Action<ILoggerFactory> configureLogging)
        {
            if (configureLogging == null)
            {
                throw new ArgumentNullException(nameof(configureLogging));
            }

            _configureLoggingDelegates.Add(configureLogging);
            return this;
        }

        /// <summary>
        /// Builds the required services and an <see cref="IHost"/> which hosts a web application.
        /// </summary>
        public IHost Build()
        {
            if (_hostBuilt)
            {
                throw new InvalidOperationException();
            }

            _hostBuilt = true;

            var hostingServices = BuildCommonServices();
            var hostingServiceProvider = hostingServices.BuildServiceProvider();

            return new Host(hostingServiceProvider, _cts);
        }

        private IServiceCollection BuildCommonServices()
        {
            var services = new ServiceCollection();

            // The configured ILoggerFactory is added as a singleton here. AddLogging below will not add an additional one.
            if (_loggerFactory == null)
            {
                _loggerFactory = new LoggerFactory();
                services.AddSingleton(provider => _loggerFactory);
            }
            else
            {
                services.AddSingleton(_loggerFactory);
            }

            foreach (var configureLogging in _configureLoggingDelegates)
            {
                configureLogging(_loggerFactory);
            }

            //This is required to add ILogger of T.
            services.AddLogging();
            services.AddOptions();
            services.AddTransient<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
            services.AddSingleton<HostedServiceExecutor>();
            services.AddSingleton<IHostLifetimeControl>(this);

            foreach (var configureServices in _configureServicesDelegates)
            {
                configureServices(services);
            }

            return services;
        }

        void IHostLifetimeControl.Shutdown()
        {
            _cts.Cancel(throwOnFirstException: false);
        }
    }
}
