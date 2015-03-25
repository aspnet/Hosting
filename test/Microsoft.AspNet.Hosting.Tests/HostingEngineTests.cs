// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.Runtime.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngineTests : IServerFactory
    {
        private readonly IList<StartInstance> _startInstances = new List<StartInstance>();

        [Fact]
        public void HostingEngineCanBeStarted()
        {
            var engineStart = HostingEngineFactory.Create(CallContextServiceLocator.Locator.ServiceProvider)
                .UseServer(this)
                .UseStartup("Microsoft.AspNet.Hosting.Tests")
                .Start();

            Assert.NotNull(engineStart);
            Assert.Equal(1, _startInstances.Count);
            Assert.Equal(0, _startInstances[0].DisposeCalls);

            engineStart.Dispose();

            Assert.Equal(1, _startInstances[0].DisposeCalls);
        }

        [Fact]
        public void CanReplaceHostingEngine()
        {
            var engine = HostingEngineFactory.Create(CallContextServiceLocator.Locator.ServiceProvider,
                services => services.AddTransient<IHostingEngine, TestEngine>());

            Assert.NotNull(engine as TestEngine);
        }

        [Fact]
        public void CanReplaceStartupLoader()
        {
            var engine = HostingEngineFactory.Create(CallContextServiceLocator.Locator.ServiceProvider,
                services => services.AddTransient<IStartupLoader, TestLoader>())
                .UseServer(this)
                .UseStartup("Microsoft.AspNet.Hosting.Tests");

            Assert.Throws<NotImplementedException>(() => engine.Start());
        }

        [Fact]
        public void CanCreateApplicationServicesWithDefaultHostingContext()
        {
            var engineStart = HostingEngineFactory.Create(CallContextServiceLocator.Locator.ServiceProvider, services => services.AddOptions());
            Assert.NotNull(engineStart.ApplicationServices.GetRequiredService<IOptions<object>>());
        }

        [Fact]
        public void EnvDefaultsToDevelopmentIfNoConfig()
        {
            var engine = HostingEngineFactory.Create(CallContextServiceLocator.Locator.ServiceProvider)
                .UseServer(this);

            using (engine.Start())
            {
                var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
                Assert.Equal("Development", env.EnvironmentName);
            }
        }

        [Fact]
        public void EnvDefaultsToDevelopmentConfigValueIfSpecified()
        {
            var vals = new Dictionary<string, string>
            {
                { "ASPNET_ENV", "Staging" }
            };

            var config = new Configuration()
                .Add(new MemoryConfigurationSource(vals));

            var engine = HostingEngineFactory.Create(CallContextServiceLocator.Locator.ServiceProvider)
                .UseConfiguration(config)
                .UseServer(this);

            using (engine.Start())
            {
                var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
                Assert.Equal("Staging", env.EnvironmentName);
            }
        }

        [Fact]
        public void WebRootCanBeResolvedFromTheProjectJson()
        {
            var engine = HostingEngineFactory.Create(CallContextServiceLocator.Locator.ServiceProvider)
                .UseServer(this);
            var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            Assert.Equal(Path.GetFullPath("testroot"), env.WebRootPath);
            Assert.True(env.WebRootFileProvider.GetFileInfo("TextFile.txt").Exists);
        }

        [Fact]
        public void IsEnvironment_Extension_Is_Case_Insensitive()
        {
            var engine = HostingEngineFactory.Create(CallContextServiceLocator.Locator.ServiceProvider)
                .UseServer(this);

            using (engine.Start())
            {
                var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
                Assert.True(env.IsEnvironment("Development"));
                Assert.True(env.IsEnvironment("developMent"));
            }
        }

        public IServerInformation Initialize(IConfiguration configuration)
        {
            return null;
        }

        public IDisposable Start(IServerInformation serverInformation, Func<IFeatureCollection, Task> application)
        {
            var startInstance = new StartInstance(application);
            _startInstances.Add(startInstance);
            return startInstance;
        }

        public class StartInstance : IDisposable
        {
            private readonly Func<IFeatureCollection, Task> _application;

            public StartInstance(Func<IFeatureCollection, Task> application)
            {
                _application = application;
            }

            public int DisposeCalls { get; set; }

            public void Dispose()
            {
                DisposeCalls += 1;
            }
        }

        private class TestLoader : IStartupLoader
        {
            public StartupMethods Load(IServiceProvider services, Type startupClass, string environmentName, IList<string> diagnosticMessages)
            {
                throw new NotImplementedException();
            }

            public StartupMethods Load(IServiceProvider services, string startupClass, string environmentName, IList<string> diagnosticMessages)
            {
                throw new NotImplementedException();
            }
        }

        private class TestEngine : IHostingEngine
        {
            public IServiceProvider ApplicationServices
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public IDisposable Start()
            {
                throw new NotImplementedException();
            }

            public IHostingEngine UseConfiguration(IConfiguration config)
            {
                throw new NotImplementedException();
            }

            public IHostingEngine UseFallbackServices(IServiceProvider services)
            {
                return this;
            }

            public IHostingEngine UseStartup<T>() where T : class
            {
                throw new NotImplementedException();
            }

            public IHostingEngine UseServer(IServerFactory factory)
            {
                throw new NotImplementedException();
            }

            public IHostingEngine UseServer(string assemblyName)
            {
                throw new NotImplementedException();
            }

            public IHostingEngine UseStartup(string startupClass)
            {
                throw new NotImplementedException();
            }

            public IHostingEngine UseStartup(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
            {
                throw new NotImplementedException();
            }

            public IHostingEngine UseStartup(Action<IApplicationBuilder> configureApp, ConfigureServicesDelegate configureServices)
            {
                throw new NotImplementedException();
            }
        }
    }
}
