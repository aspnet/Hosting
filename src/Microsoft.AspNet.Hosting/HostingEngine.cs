// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngine
    {
        private const string EnvironmentKey = "ASPNET_ENV";
        private readonly IServiceProvider _fallbackServices;
        private readonly ApplicationLifetime _appLifetime;
        private HostingEnvironment _hostingEnv;
        private IServerLoader _serverLoader;
        private IApplicationBuilderFactory _builderFactory;

        // Move everything except startupLoader to get them after configureServices
        public HostingEngine() : this(fallbackServices: null) { }

        public HostingEngine(IServiceProvider fallbackServices)
        {
            _fallbackServices = fallbackServices ?? CallContextServiceLocator.Locator.ServiceProvider;
            _appLifetime = new ApplicationLifetime();
        }

        public IDisposable Start(HostingContext context)
        {
            EnsureContextDefaults(context);
            EnsureApplicationServices(context);
            EnsureBuilder(context);
            EnsureServerFactory(context);
            InitalizeServerFactory(context);
            EnsureApplicationDelegate(context);

            var contextFactory = context.ApplicationServices.GetRequiredService<IHttpContextFactory>();
            var contextAccessor = context.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
            var server = context.ServerFactory.Start(context.Server, 
                features =>
                {
                    var httpContext = contextFactory.CreateHttpContext(features);
                    contextAccessor.HttpContext = httpContext;
                    return context.ApplicationDelegate(httpContext);
                });

            return new Disposable(() =>
            {
                _appLifetime.NotifyStopping();
                server.Dispose();
                _appLifetime.NotifyStopped();
            });
        }

        private void EnsureContextDefaults(HostingContext context)
        {
            var appEnv = _fallbackServices.GetRequiredService<IApplicationEnvironment>();
            if (context.ApplicationName == null)
            {
                context.ApplicationName = appEnv.ApplicationName;
            }
            if (context.EnvironmentName == null)
            {
                context.EnvironmentName = context.Configuration?.Get(EnvironmentKey) ?? HostingEnvironment.DefaultEnvironmentName;
            }
        }

        private void EnsureBuilder(HostingContext context)
        {
            if (context.Builder != null)
            {
                return;
            }

            EnsureApplicationServices(context);
            if (_builderFactory == null)
            {
                _builderFactory = context.ApplicationServices.GetRequiredService<IApplicationBuilderFactory>();
            }
            context.Builder = _builderFactory.CreateBuilder();
            context.Builder.ApplicationServices = context.ApplicationServices;
        }

        private void EnsureServerFactory(HostingContext context)
        {
            if (context.ServerFactory != null)
            {
                return;
            }

            if (_serverLoader == null)
            {
                EnsureApplicationServices(context);
                _serverLoader = context.ApplicationServices.GetRequiredService<IServerLoader>();
            }
            context.ServerFactory = _serverLoader.LoadServerFactory(context.ServerFactoryLocation);
        }

        private void InitalizeServerFactory(HostingContext context)
        {
            if (context.Server == null)
            {
                context.Server = context.ServerFactory.Initialize(context.Configuration);
            }

            if (context.Builder.Server == null)
            {
                context.Builder.Server = context.Server;
            }
        }

        private IServiceCollection CreateHostingServices(HostingContext context)
        {
            var services = HostingServices.Create(_fallbackServices)
                .Add(context.Services)
                .AddHosting(context.Configuration);

            // Jamming in app lifetime and hosting env since these are not replaceable
            services.AddInstance<IApplicationLifetime>(_appLifetime);
            services.AddSingleton<IHostingEnvironment, HostingEnvironment>();

            // Conjure up a RequestServices
            services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();

            return services;
        }

        private void EnsureApplicationDelegate(HostingContext context)
        {
            if (context.ApplicationDelegate != null)
            {
                return;
            }

            // REVIEW: should we call EnsureApplicationServices?
            var startupFilters = context.ApplicationServices.GetService<IEnumerable<IStartupFilter>>();
            var configure = context.StartupMethods.ConfigureDelegate;
            foreach (var filter in startupFilters)
            {
                configure = filter.Configure(context.Builder, configure);
            }

            configure(context.Builder);

            context.ApplicationDelegate = context.Builder.Build();
        }

        private void EnsureStartupMethods(HostingContext context)
        {
            if (context.StartupMethods != null)
            {
                return;
            }

            var diagnosticMessages = new List<string>();
            context.StartupMethods = ApplicationStartup.LoadStartupMethods(
                _fallbackServices,
                context.ApplicationName,
                context.EnvironmentName,
                diagnosticMessages);

            if (context.StartupMethods == null)
            {
                throw new ArgumentException(
                    diagnosticMessages.Aggregate("Failed to find an entry point for the web application.", (a, b) => a + "\r\n" + b),
                    nameof(context));
            }
        }

        private void EnsureApplicationServices(HostingContext context)
        {
            if (context.ApplicationServices != null)
            {
                return;
            }
            EnsureStartupMethods(context);
            context.ApplicationServices = context.StartupMethods.ConfigureServicesDelegate(CreateHostingServices(context));
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