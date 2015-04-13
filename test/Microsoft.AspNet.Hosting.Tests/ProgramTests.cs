// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Hosting.Tests
{
    public class ProgramTests : IServerFactory
    {
        private readonly IList<StartInstance> _startInstances = new List<StartInstance>();

        [Fact]
        public void ProgramFailsWithNoServerByDefault()
        {
            var program = new Program(CallContextServiceLocator.Locator.ServiceProvider);
            var host = program.CreateHost(new Configuration());
            var ex = Assert.Throws<InvalidOperationException>(() => host.Start());
            Assert.True(ex.Message.Contains("UseServer()"));
        }

        [Fact]
        public void ProgramWithServerStarts()
        {
            var program = new Program(CallContextServiceLocator.Locator.ServiceProvider);
            var vals = new Dictionary<string, string>
            {
                { "server", "Microsoft.AspNet.Hosting.Tests" }
            };

            var config = new Configuration()
                .Add(new MemoryConfigurationSource(vals));
            var host = program.CreateHost(config);
            host.Start();
            Assert.NotNull(host.ApplicationServices.GetRequiredService<IHostingEnvironment>());
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
    }
}
