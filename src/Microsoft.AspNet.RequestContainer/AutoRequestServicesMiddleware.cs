// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.RequestContainer
{
    public abstract class AutoRequestServicesMiddleware
    {
        private readonly IServiceProvider _services;

        public AutoRequestServicesMiddleware(RequestDelegate next, IServiceProvider services)
        {
            _services = services;
            Next = next;
        }

        protected RequestDelegate Next { get; private set; }

        public async Task Invoke(HttpContext httpContext)
        {
            // Create request services if it hasn't already been created
            if (httpContext.RequestServices == null)
            {
                using (var container = RequestServicesContainer.EnsureRequestServices(httpContext, _services))
                {
                    await InvokeCore(httpContext);
                }
            }
            else
            {
                await InvokeCore(httpContext);
            }
        }

        public abstract Task InvokeCore(HttpContext httpContext);
    }
}
