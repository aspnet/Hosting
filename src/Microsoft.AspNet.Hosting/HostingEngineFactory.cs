// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngineFactory : IHostingEngineFactory
    {
        public const string EnvironmentKey = "Hosting:Environment";

        private readonly IHostingServicesBuilder _serviceBuilder;
        private readonly IStartupLoader _startupLoader;
        private readonly IApplicationEnvironment _applicationEnvironment;
        private readonly IHostingEnvironment _hostingEnvironment;

        public HostingEngineFactory(IHostingServicesBuilder buildServices, IStartupLoader startupLoader, IApplicationEnvironment appEnv, IHostingEnvironment hostingEnv)
        {
            _serviceBuilder = buildServices;
            _startupLoader = startupLoader;
            _applicationEnvironment = appEnv;
            _hostingEnvironment = hostingEnv;
        }

        public IHostingEngine Create(IConfiguration config)
        {
            _hostingEnvironment.WebRootPath = HostingUtilities.GetWebRoot(_applicationEnvironment.ApplicationBasePath);
            _hostingEnvironment.WebRootFileProvider = new PhysicalFileProvider(_hostingEnvironment.WebRootPath);
            _hostingEnvironment.EnvironmentName = config?[EnvironmentKey] ?? _hostingEnvironment.EnvironmentName;

            return new HostingEngine(_serviceBuilder.Build(isApplicationServices: true), _startupLoader, config, _hostingEnvironment, _applicationEnvironment.ApplicationName);
        }
    }
}