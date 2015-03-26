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
            var engine = WebApplication.CreateHostingEngine(CallContextServiceLocator.Locator.ServiceProvider, new Configuration(), configureServices: null)
                .UseServer(this)
                .UseStartup("Microsoft.AspNet.Hosting.Tests")
                .Start();

            Assert.NotNull(engine);
            Assert.Equal(1, _startInstances.Count);
            Assert.Equal(0, _startInstances[0].DisposeCalls);

            engine.Dispose();

            Assert.Equal(1, _startInstances[0].DisposeCalls);
        }

        [Fact]
        public void CanReplaceHostingEngineFactory()
        {
            var factory = WebApplication.CreateHostingEngineFactory(CallContextServiceLocator.Locator.ServiceProvider,
                services => services.AddTransient<IHostingEngineFactory, TestEngineFactory>());

            Assert.NotNull(factory as TestEngineFactory);
        }

        [Fact]
        public void CanReplaceStartupLoader()
        {
            var engine = WebApplication.CreateHostingEngine(CallContextServiceLocator.Locator.ServiceProvider, new Configuration(),
                services => services.AddTransient<IStartupLoader, TestLoader>())
                .UseServer(this)
                .UseStartup("Microsoft.AspNet.Hosting.Tests");

            Assert.Throws<NotImplementedException>(() => engine.Start());
        }

        [Fact]
        public void CanCreateApplicationServices()
        {
            var engineStart = WebApplication.CreateHostingEngine(CallContextServiceLocator.Locator.ServiceProvider, new Configuration(), services => services.AddOptions());
            Assert.NotNull(engineStart.ApplicationServices.GetRequiredService<IOptions<object>>());
        }

        [Fact]
        public void EnvDefaultsToDevelopmentIfNoConfig()
        {
            var engine = WebApplication.CreateHostingEngine(CallContextServiceLocator.Locator.ServiceProvider, new Configuration(), configureServices: null)
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

            var engine = WebApplication.CreateHostingEngine(CallContextServiceLocator.Locator.ServiceProvider, config, configureServices: null)
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
            var engine = WebApplication.CreateHostingEngine(CallContextServiceLocator.Locator.ServiceProvider, config: null, configureServices: null)
                .UseServer(this);
            var env = engine.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            Assert.Equal(Path.GetFullPath("testroot"), env.WebRootPath);
            Assert.True(env.WebRootFileProvider.GetFileInfo("TextFile.txt").Exists);
        }

        [Fact]
        public void IsEnvironment_Extension_Is_Case_Insensitive()
        {
            var engine = WebApplication.CreateHostingEngine(CallContextServiceLocator.Locator.ServiceProvider, config: null, configureServices: null)
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
            public StartupMethods Load(string startupAssemblyName, string environmentName, IList<string> diagnosticMessages)
            {
                throw new NotImplementedException();
            }
        }

        private class TestEngineFactory : IHostingEngineFactory
        {
            public IHostingEngine Create(IConfiguration config)
            {
                throw new NotImplementedException();
            }
        }
    }
}
