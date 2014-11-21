// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Hosting
{
    public class Program
    {
        private const string HostingIniFile = "Microsoft.AspNet.Hosting.ini";
        private const string DefaultEnvironmentName = "Development";
        private const string EnvironmentKey = "KRE_ENV";

        private readonly IServiceProvider _serviceProvider;

        public Program(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }



        public void Main(string[] args)
        {
            var config = new Configuration();
            if (File.Exists(HostingIniFile))
            {
                config.AddIniFile(HostingIniFile);
            }
            config.AddEnvironmentVariables();
            config.AddCommandLine(args);

            var serviceCollection = HostingServices.Create(_serviceProvider, config);
            var services = serviceCollection.BuildServiceProvider();

            var appEnv = services.GetRequiredService<IApplicationEnvironment>();
            var hostingEnv = services.GetRequiredService<IHostingEnvironment>();

            var context = new HostingContext()
            {
                Services = services,
                Configuration = config,
                ServerName = config.Get("server"), // TODO: Key names
                ApplicationName = config.Get("app")  // TODO: Key names
                    ?? appEnv.ApplicationName,
                EnvironmentName = hostingEnv.EnvironmentName,
            };

            var engine = services.GetRequiredService<IHostingEngine>();
            var appShutdownService = _serviceProvider.GetRequiredService<IApplicationShutdown>();
            var shutdownHandle = new ManualResetEvent(false);

            var serverShutdown = engine.Start(context);

            appShutdownService.ShutdownRequested.Register(() =>
            {
                serverShutdown.Dispose();
                shutdownHandle.Set();
            });

            var ignored = Task.Run(() =>
            {
                var serverUrl = config.Get("server.urls");
                if (serverUrl != null)
                {
                    Console.WriteLine("Started on " + serverUrl);
                }
                else
                {
                    Console.WriteLine("Started");
                }
                Console.ReadLine();
                appShutdownService.RequestShutdown();
            });

            shutdownHandle.WaitOne();
        }
    }
}
