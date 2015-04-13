//// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
//// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

//using System;
//using Microsoft.AspNet.Hosting.Internal;
//using Microsoft.Framework.ConfigurationModel;
//using Microsoft.Framework.DependencyInjection;

//namespace Microsoft.AspNet.Hosting
//{
//    public static class WebHost
//    {
//        public static IHostingEngine CreateEngine()
//        {
//            return CreateEngine(new Configuration());
//        }

//        public static IHostingEngine CreateEngine(Action<IServiceCollection> configureServices)
//        {
//            return CreateEngine(new Configuration(), configureServices);
//        }

//        public static IHostingEngine CreateEngine(IConfiguration config)
//        {
//            return CreateEngine(config, configureServices: null);
//        }

//        public static IHostingEngine CreateEngine(IConfiguration config, Action<IServiceCollection> configureServices)
//        {
//            return CreateEngine(fallbackServices: null, config: config, configureServices: configureServices);
//        }

//        public static IHostingEngine CreateEngine(IServiceProvider fallbackServices, IConfiguration config)
//        {
//            return CreateEngine(fallbackServices, config, configureServices: null);
//        }

//        public static IHostingEngine CreateEngine(IServiceProvider fallbackServices, IConfiguration config, Action<IServiceCollection> configureServices)
//        {
//            return CreateBuilder(fallbackServices, configureServices).UseConfiguration(config).Build();
//        }

//        public static WebHostBuilder CreateBuilder()
//        {
//            return CreateBuilder(fallbackServices: null, configureServices: null);
//        }

//        public static WebHostBuilder CreateBuilder(IServiceProvider services)
//        {
//            return CreateBuilder(fallbackServices: services, configureServices: null);
//        }

//        public static WebHostBuilder CreateBuilder(Action<IServiceCollection> configureServices)
//        {
//            return CreateBuilder(fallbackServices: null, configureServices: configureServices);
//        }

//        public static WebHostBuilder CreateBuilder(IServiceProvider fallbackServices, Action<IServiceCollection> configureServices)
//        {
//            return new WebHostBuilder(fallbackServices).UseServices(configureServices);
//        }
//    }
//}