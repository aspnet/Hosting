// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public static class WebApplication
    {
        public static IHostingEngine CreateHostingEngine(IServiceProvider fallbackServices, IConfiguration config, Action<IServiceCollection> configureServices)
        {
            var factory = CreateHostingEngineFactory(fallbackServices, configureServices);
            return factory.Create(config);

        }

        public static IHostingEngineFactory CreateHostingEngineFactory(IServiceProvider fallbackServices, Action<IServiceCollection> configureServices)
        {
            var services = new HostingServicesBuilder(fallbackServices, configureServices)
                .Build(isApplicationServices: false)
                .BuildServiceProvider();
            return services
                .GetRequiredService<IHostingEngineFactory>();
        }

    }
}