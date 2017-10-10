// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting
{
    public static class WebHostBuilderExtensions
    {
        /// <summary>
        /// Specify the startup method to be used to configure the web application.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
        /// <param name="configureApp">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IWebHostBuilder Configure(this IWebHostBuilder hostBuilder, Action<IApplicationBuilder> configureApp)
        {
            if (configureApp == null)
            {
                throw new ArgumentNullException(nameof(configureApp));
            }

            var startupAssemblyName = configureApp.GetMethodInfo().DeclaringType.GetTypeInfo().Assembly.GetName().Name;

            return hostBuilder
                .UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName)
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IStartup>(sp =>
                    {
                        return new DelegateStartup(sp.GetRequiredService<IServiceProviderFactory<IServiceCollection>>(), configureApp);
                    });
                });
        }


        /// <summary>
        /// Specify the startup type to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
        /// <param name="startupType">The <see cref="Type"/> to be used.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IWebHostBuilder UseStartup(this IWebHostBuilder hostBuilder, Type startupType)
        {
            var startupAssemblyName = startupType.GetTypeInfo().Assembly.GetName().Name;

            return hostBuilder
                .UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName)
                .ConfigureServices(services =>
                {
                    if (typeof(IStartup).GetTypeInfo().IsAssignableFrom(startupType.GetTypeInfo()))
                    {
                        services.AddSingleton(typeof(IStartup), startupType);
                    }
                    else
                    {
                        services.AddSingleton(typeof(IStartup), sp =>
                        {
                            var hostingEnvironment = sp.GetRequiredService<IHostingEnvironment>();
                            return new ConventionBasedStartup(StartupLoader.LoadMethods(sp, startupType, hostingEnvironment.EnvironmentName));
                        });
                    }
                });
        }

        /// <summary>
        /// Specify the startup type to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
        /// <typeparam name ="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IWebHostBuilder UseStartup<TStartup>(this IWebHostBuilder hostBuilder) where TStartup : class
            => hostBuilder.UseStartup(typeof(TStartup));

        /// <summary>
        /// Configures the default service provider
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
        /// <param name="configure">A callback used to configure the <see cref="ServiceProviderOptions"/> for the default <see cref="IServiceProvider"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IWebHostBuilder UseDefaultServiceProvider(this IWebHostBuilder hostBuilder, Action<ServiceProviderOptions> configure)
            => hostBuilder.UseDefaultServiceProvider((context, options) => configure(options));

        /// <summary>
        /// Configures the default service provider
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
        /// <param name="configure">A callback used to configure the <see cref="ServiceProviderOptions"/> for the default <see cref="IServiceProvider"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IWebHostBuilder UseDefaultServiceProvider(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, ServiceProviderOptions> configure)
            => hostBuilder.ConfigureServices((context, services) =>
            {
                var options = new ServiceProviderOptions();
                configure(context, options);
                services.Replace(ServiceDescriptor.Singleton<IServiceProviderFactory<IServiceCollection>>(new DefaultServiceProviderFactory(options)));
            });

        /// <summary>
        /// Adds a delegate for configuring the <see cref="IConfigurationBuilder"/> that will construct an <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder" /> that will be used to construct an <see cref="IConfiguration" />.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        /// <remarks>
        /// The <see cref="IConfiguration"/> and <see cref="ILoggerFactory"/> on the <see cref="WebHostBuilderContext"/> are uninitialized at this stage.
        /// The <see cref="IConfigurationBuilder"/> is pre-populated with the settings of the <see cref="IWebHostBuilder"/>.
        /// </remarks>
        public static IWebHostBuilder ConfigureAppConfiguration(this IWebHostBuilder hostBuilder, Action<IConfigurationBuilder> configureDelegate)
            => hostBuilder.ConfigureAppConfiguration((context, builder) => configureDelegate(builder));

        /// <summary>
        /// Adds a delegate for configuring the provided <see cref="ILoggingBuilder"/>. This may be called multiple times.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder" /> to configure.</param>
        /// <param name="configureLogging">The delegate that configures the <see cref="ILoggingBuilder"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IWebHostBuilder ConfigureLogging(this IWebHostBuilder hostBuilder, Action<ILoggingBuilder> configureLogging)
            => hostBuilder.ConfigureServices(collection => collection.AddLogging(configureLogging));

        /// <summary>
        /// Adds a delegate for configuring the provided <see cref="LoggerFactory"/>. This may be called multiple times.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder" /> to configure.</param>
        /// <param name="configureLogging">The delegate that configures the <see cref="LoggerFactory"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IWebHostBuilder ConfigureLogging(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, ILoggingBuilder> configureLogging)
            => hostBuilder.ConfigureServices((context, collection) => collection.AddLogging(builder => configureLogging(context, builder)));

        /// <summary>
        /// Registers an object that will have all of its I[Post]ConfigureOptions registered.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder" /> to configure.</param>
        /// <param name="configureOptionsInstance">Object that will have all of its I[Post]ConfigureOptions registered.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IWebHostBuilder ConfigureOptions(this IWebHostBuilder hostBuilder, object configureOptionsInstance)
            => hostBuilder.ConfigureServices(services => services.ConfigureOptions(configureOptionsInstance));

        /// <summary>
        /// Registers a type that will have all of its I[Post]ConfigureOptions registered.
        /// </summary>
        /// <typeparam name="TConfigureOptions">The type that will have all of its I[Post]ConfigureOptions registered.</typeparam>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder" /> to configure.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IWebHostBuilder ConfigureOptions<TConfigureOptions>(this IWebHostBuilder hostBuilder) where TConfigureOptions : class
            => hostBuilder.ConfigureServices(services => services.ConfigureOptions<TConfigureOptions>());

        /// <summary>
        /// Registers a type that will have all of its I[Post]ConfigureOptions registered.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder" /> to configure.</param>
        /// <param name="configureOptionsType">The type that will have all of its I[Post]ConfigureOptions registered.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IWebHostBuilder ConfigureOptions(this IWebHostBuilder hostBuilder, Type configureOptionsType)
            => hostBuilder.ConfigureServices(services => services.ConfigureOptions(configureOptionsType));

    }
}
