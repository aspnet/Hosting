// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.RequestContainer;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Builder
{
    public static class ContainerExtensions
    {
        public static IApplicationBuilder UseRequestServices(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ContainerMiddleware>();
        }

        // Review: what do we use these for?
        public static IApplicationBuilder UseRequestServices(this IApplicationBuilder builder, IServiceProvider applicationServices)
        {
            // REVIEW: should this be doing fallback?
            builder.ApplicationServices = applicationServices;

            return builder.UseMiddleware<ContainerMiddleware>();
        }

        // Note: Manifests are lost after UseServices, services are flattened into ApplicationServices

        public static IApplicationBuilder UseServices(this IApplicationBuilder builder, IEnumerable<IServiceDescriptor> applicationServices)
        {
            return builder.UseServices(services => services.Add(applicationServices));
        }

        public static IApplicationBuilder UseServices(this IApplicationBuilder builder, Action<ServiceCollection> configureServices)
        {
            return builder.UseServices(serviceCollection =>
            {
                configureServices(serviceCollection);
                return serviceCollection.BuildServiceProvider();
            });
        }

        public static IApplicationBuilder UseServices(this IApplicationBuilder builder, Func<ServiceCollection, IServiceProvider> configureServices)
        {
            var serviceCollection = new ServiceCollection();

            // Import services from hosting/KRE as fallback
            serviceCollection.Import(builder.ApplicationServices);

            // TODO: should remove OptionServices here soon...
            serviceCollection.Add(OptionsServices.GetDefaultServices());
            serviceCollection.AddScoped(typeof(IContextAccessor<>), typeof(ContextAccessor<>));

            // REVIEW: serviceCollection has the merged services, manifests are lost after this
            builder.ApplicationServices = configureServices(serviceCollection);

            return builder.UseMiddleware<ContainerMiddleware>();
        }
    }
}