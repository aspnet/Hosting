// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public static class WebApplication
    {
        public static IHostingEngine CreateHostingEngine()
        {
            return CreateHostingEngine(new Configuration());
        }

        public static IHostingEngine CreateHostingEngine(Action<IServiceCollection> configureServices)
        {
            return CreateHostingEngine(new Configuration(), configureServices);
        }

        public static IHostingEngine CreateHostingEngine(IConfiguration config)
        {
            return CreateHostingEngine(config, configureServices: null);
        }

        public static IHostingEngine CreateHostingEngine(IConfiguration config, Action<IServiceCollection> configureServices)
        {
            return CreateHostingFactory(fallbackServices: null, configureServices: configureServices).Create(config);
        }

        public static IHostingEngine CreateHostingEngine(IServiceProvider fallbackServices, IConfiguration config, Action<IServiceCollection> configureServices)
        {
            return CreateHostingFactory(fallbackServices, configureServices).Create(config);
        }

        public static IHostingFactory CreateHostingFactory()
        {
            return CreateHostingFactory(fallbackServices: null, configureServices: null);
        }

        public static IHostingFactory CreateHostingFactory(Action<IServiceCollection> configureServices)
        {
            return CreateHostingFactory(fallbackServices: null, configureServices: configureServices);
        }

        public static IHostingFactory CreateHostingFactory(IServiceProvider fallbackServices, Action<IServiceCollection> configureServices)
        {
            return new HostingServicesBuilder(fallbackServices, configureServices)
                .Build()
                .BuildServiceProvider()
                .GetRequiredService<IHostingFactory>();
        }
    }
}