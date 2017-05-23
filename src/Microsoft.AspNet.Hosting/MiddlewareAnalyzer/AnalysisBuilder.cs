// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Hosting.MiddlewareAnalyzer
{
    public class AnalysisBuilder : IApplicationBuilder
    {
        public AnalysisBuilder(IApplicationBuilder inner)
        {
            InnerBuilder = inner;
        }

        private IApplicationBuilder InnerBuilder { get; }

        public IServiceProvider ApplicationServices
        {
            get { return InnerBuilder.ApplicationServices; }
            set { InnerBuilder.ApplicationServices = value; }
        }

        public IDictionary<string, object> Properties
        {
            get { return InnerBuilder.Properties; }
        }

        public IFeatureCollection ServerFeatures
        {
            get { return InnerBuilder.ServerFeatures;}
        }

        public RequestDelegate Build()
        {
            // Add one maker at the end before the default 404 middleware (or any fancy Join middleware).
            InnerBuilder.UseMiddleware<AnalysisMiddleware>("EndOfPipeline");
            // Cleanup. This is last because UseMiddleware also sets it.
            Properties.Remove(AnalysisApplicationBuilderExtensions.NextMiddlewareName);
            return InnerBuilder.Build();
        }

        public IApplicationBuilder New()
        {
            return new AnalysisBuilder(InnerBuilder.New());
        }

        public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            string middlewareName = AnalysisApplicationBuilderExtensions.GetNextMiddlewareName(this)
                ?? middleware.Target.ToString(); // Class.Method

            InnerBuilder.UseMiddleware<AnalysisMiddleware>(middlewareName);
            InnerBuilder.Use(middleware);

            // Cleanup. This is last because UseMiddleware also sets it.
            Properties.Remove(AnalysisApplicationBuilderExtensions.NextMiddlewareName);
            return this;
        }
    }
}
