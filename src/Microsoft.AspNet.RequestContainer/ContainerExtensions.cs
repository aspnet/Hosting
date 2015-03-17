// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.RequestContainer;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Builder
{
    public static class ContainerExtensions
    {
        // Note: Manifests are lost after UseServices, services are flattened into ApplicationServices

        public static IApplicationBuilder UseServices(this IApplicationBuilder builder, IServiceCollection applicationServices)
        {
            return builder.UseServices(services => services.Add(applicationServices));
        }

        public static IApplicationBuilder UseServices(this IApplicationBuilder builder, Action<IServiceCollection> configureServices)
        {
            return builder.UseServices(serviceCollection =>
            {
                configureServices(serviceCollection);
                return serviceCollection.BuildServiceProvider();
            });
        }

        public static IApplicationBuilder UseServices(this IApplicationBuilder builder, Func<IServiceCollection, IServiceProvider> configureServices)
        {
            var serviceCollection = new ServiceCollection();

            builder.ApplicationServices = configureServices(serviceCollection);

            return builder.UseMiddleware<ContainerMiddleware>();
        }
    }
}
