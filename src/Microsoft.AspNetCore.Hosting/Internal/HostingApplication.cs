// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class HostingApplication : IHttpApplication<HostingApplication.Context>
    {
        private readonly RequestDelegate _application;
        private readonly IHttpContextFactory _httpContextFactory;
        
        public HostingApplication(RequestDelegate application, IHttpContextFactory httpContextFactory)
        {
            _application = application;
            _httpContextFactory = httpContextFactory;
        }

        // Set up the request
        public Context CreateContext(IFeatureCollection contextFeatures)
        {
            var context = new Context();
            var httpContext = _httpContextFactory.Create(contextFeatures);
            context.HttpContext = httpContext;
            return context;
        }

        // Execute the request
        public Task ProcessRequestAsync(Context context)
        {
            return _application(context.HttpContext);
        }

        // Clean up the request
        public void DisposeContext(Context context, Exception exception)
        {
            var httpContext = context.HttpContext;
            _httpContextFactory.Dispose(httpContext);
        }

        public struct Context
        {
            public HttpContext HttpContext { get; set; }
            public IDisposable Scope { get; set; }
            public long StartTimestamp { get; set; }
            public bool EventLogEnabled { get; set; }
            public Activity Activity { get; set; }
        }
    }
}
