﻿using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Hosting.Tests.Fakes;
using Xunit;

namespace Microsoft.AspNet.Hosting.Tests
{
    
    public class StartupManagerTests : IFakeStartupCallback
    {
        private readonly IList<object> _configurationMethodCalledList = new List<object>();

        [Fact]
        public void DefaultServicesLocateStartupByNameAndNamespace()
        {
            IServiceProvider services = new ServiceProvider().Add(HostingServices.GetDefaultServices());

            var manager = services.GetService<IStartupManager>();

            var startup = manager.LoadStartup("Microsoft.AspNet.Hosting.Tests.Fakes.FakeStartup, Microsoft.AspNet.Hosting.Tests");

            Assert.IsType<StartupManager>(manager);
            Assert.NotNull(startup);
        }

        [Fact]
        public void StartupClassMayHaveHostingServicesInjected()
        {
            IServiceProvider services = new ServiceProvider()
                .Add(HostingServices.GetDefaultServices())
                .AddInstance<IFakeStartupCallback>(this);

            var manager = services.GetService<IStartupManager>();

            var startup = manager.LoadStartup("Microsoft.AspNet.Hosting.Tests.Fakes.FakeStartupWithServices, Microsoft.AspNet.Hosting.Tests");

            startup.Invoke(null);

            Assert.Equal(1, _configurationMethodCalledList.Count);
        }

        public void ConfigurationMethodCalled(object instance)
        {
            _configurationMethodCalledList.Add(instance);
        }
    }
}
