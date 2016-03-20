// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Startup
{
    public class StartupMethods
    {
        internal static Func<IServiceCollection, IServiceProvider> DefaultBuildServiceProvider = s => s.BuildServiceProvider();

        public StartupMethods(ConfigureDelegate configure)
            : this(configure, configureServices: null)
        {
        }

        public StartupMethods(ConfigureDelegate configure, Func<IServiceCollection, IServiceProvider> configureServices)
        {
            ConfigureDelegate = configure;
            ConfigureServicesDelegate = configureServices ?? DefaultBuildServiceProvider;
        }

        public Func<IServiceCollection, IServiceProvider> ConfigureServicesDelegate { get; }
        public ConfigureDelegate ConfigureDelegate { get; }

    }
}