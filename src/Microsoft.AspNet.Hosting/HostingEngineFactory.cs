// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingEngineFactory
    {
        public static HostingEngine Create(IServiceProvider fallbackServices)
        {
            return Create(fallbackServices, config: null, configureServices: null);
        }

        public static HostingEngine Create(IServiceProvider fallbackServices, IConfiguration config)
        {
            return Create(fallbackServices, config, configureServices: null);
        }

        public static HostingEngine Create(IServiceProvider fallbackServices, IConfiguration config, Action<IServiceCollection> configureServices)
        {
            // Replace with creating a hosting container, and creating a hosting engine from IHostingEngineFactory
            return new HostingEngine(fallbackServices, config, configureServices);
        }
    }
}