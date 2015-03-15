// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngine : IHostingEngine
    {
        private readonly IServiceProvider _fallbackServices;
        private readonly Action<IServiceCollection> _additionalServices;
        private IServerLoader _serverLoader;
        private IApplicationBuilderFactory _builderFactory;
        private IHttpContextFactory _httpContextFactory;
        private IHttpContextAccessor _contextAccessor;

        // Move everything except startupLoader to get them after configureServices
        public HostingEngine() : this(fallbackServices: null, additionalServices: null) { }

        public HostingEngine(IServiceProvider fallbackServices) : this(fallbackServices, additionalServices: null) { }

        public HostingEngine(Action<IServiceCollection> additionalServices) : this(fallbackServices : null, additionalServices: additionalServices) { }

        public HostingEngine(IServiceProvider fallbackServices, Action<IServiceCollection> additionalServices)
        {
            _fallbackServices = fallbackServices ?? CallContextServiceLocator.Locator.ServiceProvider;
            _additionalServices = additionalServices;
        }

        public IDisposable Start(HostingContext context)
        {
            EnsureApplicationStartup(context);

            EnsureApplicationServices(context);

            EnsureBuilder(context);
            EnsureServerFactory(context);
            InitalizeServerFactory(context);
            EnsureApplicationDelegate(context);

            var applicationLifetime = (ApplicationLifetime)context.ApplicationLifetime;
            var pipeline = new PipelineInstance(_httpContextFactory, context.ApplicationDelegate, _contextAccessor);
            var server = context.ServerFactory.Start(context.Server, pipeline.Invoke);

            return new Disposable(() =>
            {
                applicationLifetime.NotifyStopping();
                server.Dispose();
                pipeline.Dispose();
                applicationLifetime.NotifyStopped();
            });
        }

        private void EnsureBuilder(HostingContext context)
        {
            if (context.Builder != null)
            {
                return;
            }

            if (_builderFactory == null)
            {
                EnsureApplicationServices(context);
                _builderFactory = context.ApplicationServices.GetRequiredService<IApplicationBuilderFactory>();
            }
            context.Builder = _builderFactory.CreateBuilder();
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

        private void EnsureHostingServices(HostingContext context)
        {
            if (context.HostingServices != null)
            {
                return;
            }
            context.HostingServices = HostingServices.Create(_fallbackServices, _additionalServices)
                .AddHosting(context.Configuration);
        }

        private void EnsureApplicationDelegate(HostingContext context)
        {
            if (context.ApplicationDelegate != null)
            {
                return;
            }

            EnsureApplicationServices(context);

            context.Builder.ApplicationServices = context.ApplicationServices;

            // This will ensure RequestServices are populated, TODO implement StartupFilter
            context.Builder.UseMiddleware<RequestServicesContainerMiddleware>();

            context.ApplicationStartup.Configure(context.Builder);

            context.ApplicationDelegate = context.Builder.Build();
        }

        private void InvokeApplicationStartupFilter(HostingContext context)
        {

        }

        private void EnsureApplicationStartup(HostingContext context)
        {
            if (context.ApplicationStartup != null)
            {
                return;
            }

            var diagnosticMessages = new List<string>();
            Debugger.Launch();

            context.ApplicationStartup = ApplicationStartup.LoadStartup(
                _fallbackServices,
                context.ApplicationName,
                context.EnvironmentName,
                diagnosticMessages);

            if (context.ApplicationStartup == null)
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
            EnsureApplicationStartup(context);
            // REVIEW: revisit this
            EnsureHostingServices(context);

            context.ApplicationServices = context.ApplicationStartup.ConfigureServices(_fallbackServices, context.HostingServices);
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