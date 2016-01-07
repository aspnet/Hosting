// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Hosting
{
    public interface IWebApplicationBuilder
    {
        IWebApplication Build();

        IWebApplicationBuilder UseConfiguration(IConfiguration configuration);

        IWebApplicationBuilder UseServer(IServer server);

        IWebApplicationBuilder UseServer(IServerFactory factory);

        IWebApplicationBuilder UseStartup(Type startupType);

        IWebApplicationBuilder ConfigureServices(Action<IServiceCollection> configureServices);

        IWebApplicationBuilder Configure(Action<IApplicationBuilder> configureApp);

        IWebApplicationBuilder ConfigureLogging(Action<ILoggerFactory> configureLogging);

        IWebApplicationBuilder UseSetting(string key, string value);
    }
}