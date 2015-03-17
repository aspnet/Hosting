// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;

namespace Microsoft.Framework.DependencyInjection
{
    public static class HostingServicesExtensions
    {
        public static IServiceCollection AddHosting(this IServiceCollection services)
        {
            services.TryAdd(ServiceDescriptor.Transient<IServerLoader, ServerLoader>());

            services.TryAdd(ServiceDescriptor.Transient<IApplicationBuilderFactory, ApplicationBuilderFactory>());
            services.TryAdd(ServiceDescriptor.Transient<IHttpContextFactory, HttpContextFactory>());

            // TODO: Do we expect this to be provide by the runtime eventually?
            services.AddLogging();
            services.TryAdd(ServiceDescriptor.Singleton<IHttpContextAccessor, HttpContextAccessor>());

            return services;
        }
    }
}