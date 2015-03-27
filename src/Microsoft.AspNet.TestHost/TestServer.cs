// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Http;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.TestHost
{
    public class TestServerBuilder
    {
        public IServiceProvider FallbackServices { get; set; }
        public string Environment { get; set; }

        public Type StartupType { get; set; }
        public string StartupAssemblyName { get; set; }
        public IConfiguration Config { get; set; }

        public IServiceCollection AdditionalServices { get; } = new ServiceCollection();

        public StartupMethods Startup { get; set; }

        public TestServer Build()
        {
            var fallbackServices = FallbackServices ?? CallContextServiceLocator.Locator.ServiceProvider;
            var config = Config ?? new Configuration();
            if (Environment != null)
            {
                config[HostingFactory.EnvironmentKey] = Environment;
            }

            var engine = WebApplication.CreateHostingEngine(fallbackServices,
                config,
                services => services.Add(AdditionalServices));
            // REVIEW: startup type overrides Startup delegates that were set
            if (StartupType != null)
            {
                Startup = new StartupLoader(fallbackServices).Load(StartupType, Environment, new List<string>());
            }
            if (Startup != null)
            {
                engine.UseStartup(Startup.ConfigureDelegate, Startup.ConfigureServicesDelegate);
            }
            else if (StartupAssemblyName != null)
            {
                engine.UseStartup(StartupAssemblyName);
            }

            return new TestServer(engine);
        }
    }

    public class TestServer : IServerFactory, IDisposable
    {
        private const string DefaultEnvironmentName = "Development";
        private const string ServerName = nameof(TestServer);
        private static readonly ServerInformation ServerInfo = new ServerInformation();
        private Func<IFeatureCollection, Task> _appDelegate;
        private IDisposable _appInstance;
        private bool _disposed = false;

        public TestServer(IHostingEngine engine)
        {
            _appInstance = engine.UseServer(this).Start();
        }

        public Uri BaseAddress { get; set; } = new Uri("http://localhost/");

        public static TestServer Create()
        {
            return Create(fallbackServices: null, config: null, configureApp: null, configureServices: null);
        }

        public static TestServer Create(Action<IApplicationBuilder> configureApp)
        {
            return Create(fallbackServices: null, config: null, configureApp: configureApp, configureServices: null);
        }

        public static TestServer Create(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
            return Create(fallbackServices: null, config: null, configureApp: configureApp, configureServices: configureServices);
        }

        // REVIEW: see if can we live without these overloads
        //public static TestServer Create(IServiceProvider fallbackServices, Action<IApplicationBuilder> configureApp)
        //{
        //    return Create(fallbackServices, config: null, configureApp: configureApp, configureServices: null);
        //}

        //public static TestServer Create(IServiceProvider fallbackServices, IConfiguration config)
        //{
        //    return Create(fallbackServices, config: null, configureApp: null, configureServices: null);
        //}

        public static TestServer Create(IServiceProvider fallbackServices, IConfiguration config, Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
            return CreateBuilder(fallbackServices, config, configureApp, configureServices).Build();
        }

        public static TestServer Create<TStartup>() where TStartup : class
        {
            return Create<TStartup>(fallbackServices: null, config: null, configureApp: null, configureServices: null);
        }

        public static TestServer Create<TStartup>(Action<IApplicationBuilder> configureApp) where TStartup : class
        {
            return Create<TStartup>(fallbackServices: null, config: null, configureApp: configureApp, configureServices: null);
        }

        public static TestServer Create<TStartup>(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices) where TStartup : class
        {
            return Create<TStartup>(fallbackServices: null, config: null, configureApp: configureApp, configureServices: configureServices);
        }

        public static TestServer Create<TStartup>(IServiceProvider fallbackServices, IConfiguration config, Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices) where TStartup : class
        {
            var builder = CreateBuilder(fallbackServices, config, configureApp, configureServices);
            builder.StartupType = typeof(TStartup);
            return builder.Build();
        }

        public static TestServerBuilder CreateBuilder<TStartup>() where TStartup : class
        {
            var builder = CreateBuilder(fallbackServices: null, config: null, configureApp: null, configureServices: null);
            builder.StartupType = typeof(TStartup);
            return builder;
        }

        public static TestServerBuilder CreateBuilder<TStartup>(IServiceProvider fallbackServices, IConfiguration config, Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices) where TStartup : class
        {
            var builder = CreateBuilder(fallbackServices, config, configureApp, configureServices);
            builder.StartupType = typeof(TStartup);
            return builder;
        }

        public static TestServerBuilder CreateBuilder(IServiceProvider fallbackServices, IConfiguration config, Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
            return new TestServerBuilder
            {
                FallbackServices = fallbackServices,
                Startup = new StartupMethods(configureApp, services =>
                {
                    if (configureServices != null)
                    {
                        configureServices(services);
                    }
                    return services.BuildServiceProvider();
                }),
                Config = config
            };
        }

        public HttpMessageHandler CreateHandler()
        {
            var pathBase = BaseAddress == null ? PathString.Empty : PathString.FromUriComponent(BaseAddress);
            return new ClientHandler(Invoke, pathBase);
        }

        public HttpClient CreateClient()
        {
            return new HttpClient(CreateHandler()) { BaseAddress = BaseAddress };
        }

        /// <summary>
        /// Begins constructing a request message for submission.
        /// </summary>
        /// <param name="path"></param>
        /// <returns><see cref="RequestBuilder"/> to use in constructing additional request details.</returns>
        public RequestBuilder CreateRequest(string path)
        {
            return new RequestBuilder(this, path);
        }

        public IServerInformation Initialize(IConfiguration configuration)
        {
            return ServerInfo;
        }

        public IDisposable Start(IServerInformation serverInformation, Func<IFeatureCollection, Task> application)
        {
            if (!(serverInformation.GetType() == typeof(ServerInformation)))
            {
                throw new ArgumentException(string.Format("The server must be {0}", ServerName), "serverInformation");
            }

            _appDelegate = application;

            return this;
        }

        public Task Invoke(IFeatureCollection featureCollection)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            return _appDelegate(featureCollection);
        }

        public void Dispose()
        {
            _disposed = true;
            _appInstance.Dispose();
        }

        private class ServerInformation : IServerInformation
        {
            public string Name
            {
                get { return ServerName; }
            }
        }
    }
}