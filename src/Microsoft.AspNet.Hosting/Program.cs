// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Hosting
{
    public class Program
    {
        private const string HostingJsonFile = "Microsoft.AspNet.Hosting.json";

        private readonly IServiceProvider _serviceProvider;

        public Program(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Main(string[] args)
        {
            var config = new Configuration();
            if (File.Exists(HostingJsonFile))
            {
                config.AddJsonFile(HostingJsonFile);
            }
            config.AddEnvironmentVariables();
            config.AddCommandLine(args);

            var host = new WebHostBuilder(_serviceProvider, config).Build();
            using (host.Start())
            {
                Console.WriteLine("Started");
                var appShutdownService = host.ApplicationServices.GetRequiredService<IApplicationShutdown>();
                Console.CancelKeyPress += delegate { appShutdownService.RequestShutdown(); };
                appShutdownService.ShutdownRequested.WaitHandle.WaitOne();
            }
        }
    }
}
