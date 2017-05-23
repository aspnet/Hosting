// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Hosting.MiddlewareAnalyzer
{
    public class AnalysisMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DiagnosticSource _diagnostics;
        private readonly string _middlewareName;

        public AnalysisMiddleware(RequestDelegate next, DiagnosticSource diagnosticSource, string middlewareName)
        {
            _next = next;
            _diagnostics = diagnosticSource;
            _middlewareName = middlewareName;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (_diagnostics.IsEnabled("Microsoft.AspNet.Hosting.MiddlewareStarting"))
            {
                _diagnostics.Write("Microsoft.AspNet.Hosting.MiddlewareStarting", new { name = _middlewareName, httpContext = httpContext, tickCount = Environment.TickCount });
            }

            // TODO: What about OnStarting?

            try
            {
                await _next(httpContext);

                if (_diagnostics.IsEnabled("Microsoft.AspNet.Hosting.MiddlewareFinished"))
                {
                    _diagnostics.Write("Microsoft.AspNet.Hosting.MiddlewareFinished", new { name = _middlewareName, httpContext = httpContext, tickCount = Environment.TickCount });
                }
            }
            catch (Exception ex)
            {
                if (_diagnostics.IsEnabled("Microsoft.AspNet.Hosting.MiddlewareException"))
                {
                    _diagnostics.Write("Microsoft.AspNet.Hosting.MiddlewareException", new { name = _middlewareName, exception = ex, tickCount = Environment.TickCount });
                }
                throw;
            }
        }
    }
}
