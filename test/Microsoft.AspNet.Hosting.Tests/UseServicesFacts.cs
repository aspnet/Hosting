// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using Xunit;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Hosting.Tests
{
    public class UseServicesFacts
    {
        [Fact]
        public void OptionsAccessorCanBeResolvedAfterCallingUseServicesWithAction()
        {
            var baseServiceProvider = new ServiceCollection().BuildServiceProvider();
            var builder = new ApplicationBuilder(baseServiceProvider);

            builder.UseServices(serviceCollection => { });

            var optionsAccessor = builder.ApplicationServices.GetRequiredService<IOptions<object>>();
            Assert.NotNull(optionsAccessor);
        }


        [Fact]
        public void OptionsAccessorCanBeResolvedAfterCallingUseServicesWithFunc()
        {
            var baseServiceProvider = new ServiceCollection().BuildServiceProvider();
            var builder = new ApplicationBuilder(baseServiceProvider);
            IServiceProvider serviceProvider = null;

            builder.UseServices(serviceCollection =>
            {
                serviceProvider = serviceCollection.BuildServiceProvider();
                return serviceProvider;
            });

            Assert.Same(serviceProvider, builder.ApplicationServices);
            var optionsAccessor = builder.ApplicationServices.GetRequiredService<IOptions<object>>();
            Assert.NotNull(optionsAccessor);
        }

        [Theory]
        [InlineData(typeof(IHostingEngine))]
        [InlineData(typeof(IServerManager))]
        [InlineData(typeof(IStartupManager))]
        [InlineData(typeof(IStartupLoaderProvider))]
        [InlineData(typeof(IApplicationBuilderFactory))]
        [InlineData(typeof(IStartupLoaderProvider))]
        [InlineData(typeof(IHttpContextFactory))]
        [InlineData(typeof(ITypeActivator))]
        [InlineData(typeof(IApplicationLifetime))]
        [InlineData(typeof(ILoggerFactory))]
        public void UseServicesHostingImportedServicesAreDefined(Type service)
        {
            var baseServiceProvider = new ServiceCollection().Add(HostingServices.GetDefaultServices()).BuildFallbackServiceProvider();
            var builder = new ApplicationBuilder(baseServiceProvider);

            builder.UseServices(serviceCollection => { });

            Assert.NotNull(builder.ApplicationServices.GetRequiredService(service));
        }
    }
}