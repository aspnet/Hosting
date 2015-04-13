// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Hosting
{
    public class Program
    {
        private const string HostingIniFile = "Microsoft.AspNet.Hosting.ini";

        private readonly IServiceProvider _serviceProvider;

        public Program(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IHostingEngine CreateEngine(IConfiguration config)
        {

            var engine = WebHost.CreateEngine(_serviceProvider, config);
            var server = config.Get("server");
            if (server != null)
            {
                engine.UseServer(server);
            }
            var startup = config.Get("app");
            if (startup != null)
            {
                engine.UseStartup(startup);
            }
            return engine;
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

            var engine = CreateEngine(config);
            var serverShutdown = engine.Start();
            var loggerFactory = engine.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var appShutdownService = engine.ApplicationServices.GetRequiredService<IApplicationShutdown>();
            var shutdownHandle = new ManualResetEvent(false);

            appShutdownService.ShutdownRequested.Register(() =>
            {
                try
                {
                    serverShutdown.Dispose();
                }
                catch (Exception ex)
                {
                    var logger = loggerFactory.CreateLogger<Program>();
                    logger.LogError("Dispose threw an exception.", ex);
                }
                shutdownHandle.Set();
            });

            var ignored = Task.Run(() =>
            {
                Console.WriteLine("Started");
                Console.ReadLine();
                appShutdownService.RequestShutdown();
            });

            shutdownHandle.WaitOne();
        }
    }
}
