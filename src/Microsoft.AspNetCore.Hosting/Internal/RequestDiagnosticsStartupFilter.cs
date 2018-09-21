// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class RequestDiagnosticsStartupFilter : IStartupFilter
    {
        private readonly DiagnosticListener _diagnosticListener;
        private readonly ILoggerFactory _loggerFactory;

        public RequestDiagnosticsStartupFilter(DiagnosticListener diagnosticListener, ILoggerFactory loggerFactory)
        {
            _diagnosticListener = diagnosticListener;
            _loggerFactory = loggerFactory;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                // This logging category is for compatibility
                var logger = _loggerFactory.CreateLogger<WebHost>();
                builder.UseMiddleware<RequestDiagnosticsMiddleware>(_diagnosticListener, logger);
                next(builder);
            };
        }
    }
}
