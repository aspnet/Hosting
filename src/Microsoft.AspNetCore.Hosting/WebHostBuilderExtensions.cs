// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

            return hostBuilder.UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName)
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

            return hostBuilder.UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName)
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
        {
            return hostBuilder.UseStartup(typeof(TStartup));
        }

        /// <summary>
        /// Configures the default service provider
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
        /// <param name="configure">A callback used to configure the <see cref="ServiceProviderOptions"/> for the default <see cref="IServiceProvider"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IWebHostBuilder UseDefaultServiceProvider(this IWebHostBuilder hostBuilder, Action<ServiceProviderOptions> configure)
        {
            return hostBuilder.ConfigureServices(services =>
            {
                var options = new ServiceProviderOptions();
                configure(options);
                services.Replace(ServiceDescriptor.Singleton<IServiceProviderFactory<IServiceCollection>>(new DefaultServiceProviderFactory(options)));
            });
        }

        /// <summary>
        /// Runs a web application with the specified handler
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to run.</param>
        /// <param name="handler">A delegate that handles the request.</param>
        public static IWebHost Run(this IWebHostBuilder hostBuilder, RequestDelegate handler)
        {
            var host = hostBuilder.Configure(app => app.Run(handler)).Build();
            host.Start();
            return host;
        }

        /// <summary>
        /// Runs a web application with the specified handler
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to run.</param>
        /// <param name="hostname">The host name to bind to.</param>
        /// <param name="port">The port to bind to.</param>
        /// <param name="handler">A delegate that handles the request.</param>
        public static IWebHost Run(this IWebHostBuilder hostBuilder, string hostname, int port, RequestDelegate handler)
        {
            var host = hostBuilder.UseUrls($"http://{hostname}:{port}/")
                                  .Configure(app => app.Run(handler))
                                  .Build();
            host.Start();
            return host;
        }

        /// <summary>
        /// Runs a web application with the specified handler
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to run.</param>
        /// <param name="configure">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
        public static IWebHost RunApplication(this IWebHostBuilder hostBuilder, Action<IApplicationBuilder> configure)
        {
            var host = hostBuilder.Configure(configure).Build();
            host.Start();
            return host;
        }

        /// <summary>
        /// Runs a web application with the specified handler
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to run.</param>
        /// <param name="hostname">The host name to bind to.</param>
        /// <param name="port">The port to bind to.</param>
        /// <param name="configure">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
        public static IWebHost RunApplication(this IWebHostBuilder hostBuilder, string hostname, int port, Action<IApplicationBuilder> configure)
        {
            var host = hostBuilder.UseUrls($"http://{hostname}:{port}/")
                                  .Configure(configure)
                                  .Build();
            host.Start();
            return host;
        }
    }
}