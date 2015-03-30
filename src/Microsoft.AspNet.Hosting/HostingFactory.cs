// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Hosting
{
    public class HostingFactory : IHostingFactory
    {
        public const string EnvironmentKey = "ASPNET_ENV";

        private readonly IHostingServicesBuilder _serviceBuilder;
        private readonly IStartupLoader _startupLoader;
        private readonly IApplicationEnvironment _applicationEnvironment;
        private readonly IHostingEnvironment _hostingEnvironment;

        public HostingFactory(IHostingServicesBuilder buildServices, IStartupLoader startupLoader, IApplicationEnvironment appEnv, IHostingEnvironment hostingEnv)
        {
            _serviceBuilder = buildServices;
            _startupLoader = startupLoader;
            _applicationEnvironment = appEnv;
            _hostingEnvironment = hostingEnv;
        }

        public IHostingEngine Create(IConfiguration config)
        {
            _hostingEnvironment.EnvironmentName = config?[EnvironmentKey] ?? _hostingEnvironment.EnvironmentName;

            return new HostingEngine(_serviceBuilder.Build(), _startupLoader, config, _hostingEnvironment, _applicationEnvironment.ApplicationName);
        }
    }
}