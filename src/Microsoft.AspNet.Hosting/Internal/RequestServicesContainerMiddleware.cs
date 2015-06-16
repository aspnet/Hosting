// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Hosting.Internal
{
    public class RequestServicesContainerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _services;

        public RequestServicesContainerMiddleware([NotNull] RequestDelegate next, [NotNull] IServiceProvider services)
        {
            _services = services;
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            // All done if we already have a request services
            if (httpContext.RequestServices != null)
            {
                return;
            }

            var serviceProvider = httpContext.ApplicationServices ?? _services;

            var appServiceProvider = serviceProvider.GetRequiredService<IServiceProvider>();
            var appServiceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            if (serviceProvider != appServiceProvider)
            {
                appServiceProvider = serviceProvider;
                appServiceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            }

            try
            {
                // Creates the scope and tempororarily swap services
                using (var scope = appServiceScopeFactory.CreateScope())
                {
                    httpContext.ApplicationServices = appServiceProvider;
                    httpContext.RequestServices = scope.ServiceProvider;

                    await _next.Invoke(httpContext);
                }
            }
            finally
            {
                httpContext.RequestServices = serviceProvider; // REVIEW: should this go back to null instead?
                httpContext.ApplicationServices = serviceProvider;
            }
        }
    }
}
