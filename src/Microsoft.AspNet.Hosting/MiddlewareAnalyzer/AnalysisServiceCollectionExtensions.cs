﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Hosting.MiddlewareAnalyzer;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AnalysisServiceCollectionExtensions
    {
        public static IServiceCollection AddMiddlewareAnalysis(this IServiceCollection services)
        {
            // This should prevent AnalysisStartupFilter from being registered more than once.
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, AnalysisStartupFilter>());
            return services;
        }
    }
}
