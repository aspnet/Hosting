// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class RequestDiagnosticsMiddleware
    {
        private readonly RequestDelegate _next;
        private HostingApplicationDiagnostics _diagnostics;

        public RequestDiagnosticsMiddleware(RequestDelegate next, DiagnosticListener diagnosticListener, ILogger logger)
        {
            _next = next;
            _diagnostics = new HostingApplicationDiagnostics(logger, diagnosticListener);
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var context = new HostingApplication.Context();
            _diagnostics.BeginRequest(httpContext, ref context);

            try
            {
                await _next.Invoke(httpContext);

                _diagnostics.RequestEnd(httpContext, null, context);
            }
            catch (Exception exception)
            {
                _diagnostics.RequestEnd(httpContext, exception, context);
                throw;
            }
        }
    }
}