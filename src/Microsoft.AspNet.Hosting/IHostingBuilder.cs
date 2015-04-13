// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public interface IHostingBuilder
    {
        IHostingEngine Build(IConfiguration config);

        IHostingBuilder UseEnvironment(string environment);

        // Mutually exclusive
        IHostingBuilder UseServer(string assemblyName);
        IHostingBuilder UseServer(IServerFactory factory);

        // Mutually exclusive
        IHostingBuilder UseStartup(string startupAssemblyName);
        IHostingBuilder UseStartup(Action<IApplicationBuilder> configureApp);
        IHostingBuilder UseStartup(Action<IApplicationBuilder> configureApp, ConfigureServicesDelegate configureServices);
        IHostingBuilder UseStartup(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices);
    }
}