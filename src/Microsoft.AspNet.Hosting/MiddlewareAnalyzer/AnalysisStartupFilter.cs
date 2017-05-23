// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Startup;

namespace Microsoft.AspNet.Hosting.MiddlewareAnalyzer
{
    public class AnalysisStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                var wrappedBuilder = new AnalysisBuilder(builder);
                next(wrappedBuilder);

                // The caller doesn't call build on our new builder, they call it on the original. Add this
                // default middleware to the end. Compare with AnalysisBuilder.Build();

                // Add one maker at the end before the default 404 middleware (or any fancy Join middleware).
                builder.UseMiddleware<AnalysisMiddleware>("EndOfPipeline");
                // Cleanup. This is last because UseMiddleware also sets it.
                builder.Properties.Remove(AnalysisApplicationBuilderExtensions.NextMiddlewareName);
            };
        }
    }
}
