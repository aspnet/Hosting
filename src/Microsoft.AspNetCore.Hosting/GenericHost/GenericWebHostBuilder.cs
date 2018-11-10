﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal class GenericWebHostBuilder : IWebHostBuilder
    {
        private readonly IHostBuilder _builder;
        private readonly IConfiguration _config;
        private readonly object _startupKey = new object();
        private AggregateException _hostingStartupErrors;

        public GenericWebHostBuilder(IHostBuilder builder)
        {
            _builder = builder;
            _config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .Build();

            _builder.ConfigureHostConfiguration(config =>
            {
                config.AddConfiguration(_config);
            });

            ExecuteHostingStartups();

            _builder.ConfigureServices((context, services) =>
            {
                var webhostContext = GetWebHostBuilderContext(context);
                var webHostOptions = (WebHostOptions)context.Properties[typeof(WebHostOptions)];

                // Add the IHostingEnvironment and IApplicationLifetime from Microsoft.AspNetCore.Hosting
                services.AddSingleton(webhostContext.HostingEnvironment);
                services.AddSingleton<IApplicationLifetime, GenericHostApplicationLifetime>();

                services.Configure<GenericWebHostServiceOptions>(options =>
                {
                    // Set the options
                    options.WebHostOptions = webHostOptions;
                    // Store and forward any startup errors
                    options.HostingStartupExceptions = _hostingStartupErrors;
                });

                services.AddHostedService<GenericWebHostService>();

                // REVIEW: This is bad since we don't own this type. Anybody could add one of these and it would mess things up
                // We need to flow this differently
                var listener = new DiagnosticListener("Microsoft.AspNetCore");
                services.TryAddSingleton<DiagnosticListener>(listener);
                services.TryAddSingleton<DiagnosticSource>(listener);

                services.TryAddSingleton<IHttpContextFactory, HttpContextFactory>();
                services.TryAddScoped<IMiddlewareFactory, MiddlewareFactory>();
                services.TryAddSingleton<IApplicationBuilderFactory, ApplicationBuilderFactory>();

                // Conjure up a RequestServices
                services.TryAddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();
                services.TryAddTransient<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();

                // Ensure object pooling is available everywhere.
                services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

                // Support UseStartup(assemblyName)
                if (!string.IsNullOrEmpty(webHostOptions.StartupAssembly))
                {
                    try
                    {
                        var startupType = StartupLoader.FindStartupType(webHostOptions.StartupAssembly, webhostContext.HostingEnvironment.EnvironmentName);
                        UseStartup(startupType);
                    }
                    catch (Exception ex) when (webHostOptions.CaptureStartupErrors)
                    {
                        var capture = ExceptionDispatchInfo.Capture(ex);

                        services.Configure<GenericWebHostServiceOptions>(options =>
                        {
                            options.ConfigureApplication = app =>
                            {
                                // Throw if there was any errors initializing startup
                                capture.Throw();
                            };
                        });
                    }
                }
            });
        }

        private void ExecuteHostingStartups()
        {
            // REVIEW: This doesn't support arbitrary hosting configuration. We need to run this during the call to ConfigureWebHost
            // not during IHostBuilder.Build(). This is because IHostingStartup.Configure mutates the builder itself and that should happen *before*
            // the delegate execute, not during.
            var configuration = new ConfigurationBuilder()
                            .AddEnvironmentVariables()
                            .Build();

            // REVIEW: Is this application name correct?
            var webHostOptions = new WebHostOptions(configuration, Assembly.GetEntryAssembly()?.GetName().Name);

            if (!webHostOptions.PreventHostingStartup)
            {
                var exceptions = new List<Exception>();

                // Execute the hosting startup assemblies
                foreach (var assemblyName in webHostOptions.GetFinalHostingStartupAssemblies().Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    try
                    {
                        var assembly = Assembly.Load(new AssemblyName(assemblyName));

                        foreach (var attribute in assembly.GetCustomAttributes<HostingStartupAttribute>())
                        {
                            var hostingStartup = (IHostingStartup)Activator.CreateInstance(attribute.HostingStartupType);
                            hostingStartup.Configure(this);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Capture any errors that happen during startup
                        exceptions.Add(new InvalidOperationException($"Startup assembly {assemblyName} failed to execute. See the inner exception for more details.", ex));
                    }
                }

                if (exceptions.Count > 0)
                {
                    _hostingStartupErrors = new AggregateException(exceptions);
                }
            }
        }

        public IWebHost Build() => throw new NotSupportedException($"Building this implementation of {nameof(IWebHostBuilder)} is not supported.");

        public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _builder.ConfigureAppConfiguration((context, builder) =>
            {
                var webhostBuilderContext = GetWebHostBuilderContext(context);
                configureDelegate(webhostBuilderContext, builder);
            });

            return this;
        }

        public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            return ConfigureServices((context, services) => configureServices(services));
        }

        public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
        {
            _builder.ConfigureServices((context, builder) =>
            {
                var webhostBuilderContext = GetWebHostBuilderContext(context);
                configureServices(webhostBuilderContext, builder);
            });

            return this;
        }

        internal IWebHostBuilder UseDefaultServiceProvider(Action<WebHostBuilderContext, ServiceProviderOptions> configure)
        {
            // REVIEW: This is a hack to change the builder with the HostBuilderContext in scope,
            // we're not actually using configuration here
            _builder.ConfigureAppConfiguration((context, _) =>
            {
                var webHostBuilderContext = GetWebHostBuilderContext(context);
                var options = new ServiceProviderOptions();
                configure(webHostBuilderContext, options);

                // This is only fine because this runs last
                _builder.UseServiceProviderFactory(new DefaultServiceProviderFactory(options));
            });

            return this;
        }

        internal IWebHostBuilder UseStartup(Type startupType)
        {
            _config[HostDefaults.ApplicationKey] = startupType.GetTypeInfo().Assembly.GetName().Name;

            _builder.ConfigureServices((context, services) =>
            {
                var webHostBuilderContext = GetWebHostBuilderContext(context);
                var webHostOptions = (WebHostOptions)context.Properties[typeof(WebHostOptions)];

                ExceptionDispatchInfo startupError = null;
                object instance = null;
                ConfigureBuilder configureBuilder = null;

                try
                {
                    // We cannot support methods that return IServiceProvider as that is terminal and we need ConfigureServices to compose
                    if (typeof(IStartup).IsAssignableFrom(startupType))
                    {
                        throw new NotSupportedException($"{typeof(IStartup)} isn't supported");
                    }

                    instance = ActivatorUtilities.CreateInstance(new ServiceProvider(webHostBuilderContext), startupType);
                    context.Properties[_startupKey] = instance;

                    // Startup.ConfigureServices
                    var configureServicesBuilder = StartupLoader.FindConfigureServicesDelegate(startupType, context.HostingEnvironment.EnvironmentName);
                    var configureServices = configureServicesBuilder.Build(instance);

                    configureServices(services);

                    // REVIEW: We're doing this in the callback so that we have access to the hosting environment
                    // Startup.ConfigureContainer
                    var configureContainerBuilder = StartupLoader.FindConfigureContainerDelegate(startupType, context.HostingEnvironment.EnvironmentName);
                    if (configureContainerBuilder.MethodInfo != null)
                    {
                        var containerType = configureContainerBuilder.GetContainerType();
                        // Store the builder in the property bag
                        _builder.Properties[typeof(ConfigureContainerBuilder)] = configureContainerBuilder;

                        var actionType = typeof(Action<,>).MakeGenericType(typeof(HostBuilderContext), containerType);

                        // Get the private ConfigureContainer method on this type then close over the container type
                        var configureCallback = GetType().GetMethod(nameof(ConfigureContainer), BindingFlags.NonPublic | BindingFlags.Instance)
                                                         .MakeGenericMethod(containerType)
                                                         .CreateDelegate(actionType, this);

                        // _builder.ConfigureContainer<T>(ConfigureContainer);
                        typeof(IHostBuilder).GetMethods().First(m => m.Name == nameof(IHostBuilder.ConfigureContainer))
                            .MakeGenericMethod(containerType)
                            .Invoke(_builder, new object[] { configureCallback });
                    }

                    // Resolve Configure after calling ConfigureServices and ConfigureContainer
                    configureBuilder = StartupLoader.FindConfigureDelegate(startupType, context.HostingEnvironment.EnvironmentName);
                }
                catch (Exception ex) when (webHostOptions.CaptureStartupErrors)
                {
                    startupError = ExceptionDispatchInfo.Capture(ex);
                }

                // Startup.Configure
                services.Configure<GenericWebHostServiceOptions>(options =>
                {
                    options.ConfigureApplication = app =>
                    {
                        // Throw if there was any errors initializing startup
                        startupError?.Throw();

                        // Execute Startup.Configure
                        if (instance != null && configureBuilder != null)
                        {
                            configureBuilder.Build(instance)(app);
                        }
                    };
                });
            });

            return this;
        }

        private void ConfigureContainer<TContainer>(HostBuilderContext context, TContainer container)
        {
            var instance = context.Properties[_startupKey];
            var builder = (ConfigureContainerBuilder)context.Properties[typeof(ConfigureContainerBuilder)];
            builder.Build(instance)(container);
        }

        internal IWebHostBuilder Configure(Action<IApplicationBuilder> configure)
        {
            _builder.ConfigureServices((context, services) =>
            {
                services.Configure<GenericWebHostServiceOptions>(options =>
                {
                    options.ConfigureApplication = configure;
                });
            });

            return this;
        }

        private WebHostBuilderContext GetWebHostBuilderContext(HostBuilderContext context)
        {
            if (!context.Properties.TryGetValue(typeof(WebHostBuilderContext), out var contextVal))
            {
                var options = new WebHostOptions(_config, Assembly.GetEntryAssembly()?.GetName().Name);
                var hostingEnvironment = new HostingEnvironment();
                hostingEnvironment.Initialize(context.HostingEnvironment.ContentRootPath, options);

                var webHostBuilderContext = new WebHostBuilderContext
                {
                    Configuration = context.Configuration,
                    HostingEnvironment = hostingEnvironment
                };
                context.Properties[typeof(WebHostBuilderContext)] = webHostBuilderContext;
                context.Properties[typeof(WebHostOptions)] = options;
                return webHostBuilderContext;
            }

            return (WebHostBuilderContext)contextVal;
        }

        public string GetSetting(string key)
        {
            return _config[key];
        }

        public IWebHostBuilder UseSetting(string key, string value)
        {
            _config[key] = value;
            return this;
        }

        // This exists just so that we can use ActivatorUtilities.CreateInstance on the Startup class
        private class ServiceProvider : IServiceProvider
        {
            private readonly WebHostBuilderContext _context;

            public ServiceProvider(WebHostBuilderContext context)
            {
                _context = context;
            }

            public object GetService(Type serviceType)
            {
                // The implementation of the HostingEnvironment supports both interfaces
                if (serviceType == typeof(Microsoft.AspNetCore.Hosting.IHostingEnvironment) || serviceType == typeof(IHostingEnvironment))
                {
                    return _context.HostingEnvironment;
                }

                if (serviceType == typeof(IConfiguration))
                {
                    return _context.Configuration;
                }

                return null;
            }
        }
    }
}