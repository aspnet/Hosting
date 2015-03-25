// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Http;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public interface IHostingEngine
    {
        IDisposable Start();
        void Dispose();

        // Accessing this will block Use methods
        IServiceProvider ApplicationServices { get; }

        // Use methods blow up after any of the above methods are called

        IHostingEngine UseFallbackServices(IServiceProvider services);

        IHostingEngine UseConfiguration(IConfiguration config);

        // Mutually exclusive
        IHostingEngine UseServer(string assemblyName);
        IHostingEngine UseServer(IServerFactory factory);

        // Mutually exclusive
        IHostingEngine UseStartup(string startupName);
        IHostingEngine UseStartup<T>() where T : class;
        IHostingEngine UseStartup(Type startupType);
        IHostingEngine UseStartup(Action<IApplicationBuilder> configureApp, ConfigureServicesDelegate configureServices);
        IHostingEngine UseStartup(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices);
    }
}