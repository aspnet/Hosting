// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if NETSTANDARD1_5
using System.Reflection;
using System.Runtime.Loader;
#endif
using System.Threading;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    public static class WebHostExtensions
    {
        /// <summary>
        /// Blocks the calling thread until the web application shuts down.
        /// </summary>
        /// <param name="host">The <see cref="IWebHost"/> to wait for shutdown</param>
        public static void WaitForShutdown(this IWebHost host)
        {
            WaitForSystemShutdown(host, (token, message) => host.WaitForShutdown(token, message));
        }

        /// <summary>
        /// Blocks the calling thread until the web application shuts down.
        /// </summary>
        /// <param name="host">The <see cref="IWebHost"/> to wait for shutdown</param>
        /// <param name="token">The token to trigger shutdown.</param>
        public static void WaitForShutdown(this IWebHost host, CancellationToken token)
        {
            host.WaitForShutdown(token, shutdownMessage: null);
        }

        /// <summary>
        /// Runs a web application and block the calling thread until host shutdown.
        /// </summary>
        /// <param name="host">The <see cref="IWebHost"/> to run.</param>
        public static void Run(this IWebHost host)
        {
            WaitForSystemShutdown(host, (token, message) => host.Run(token, message));
        }

        /// <summary>
        /// Runs a web application and block the calling thread until token is triggered or shutdown is triggered.
        /// </summary>
        /// <param name="host">The <see cref="IWebHost"/> to run.</param>
        /// <param name="token">The token to trigger shutdown.</param>
        public static void Run(this IWebHost host, CancellationToken token)
        {
            host.Run(token, shutdownMessage: null);
        }

        private static void Run(this IWebHost host, CancellationToken token, string shutdownMessage)
        {
            using (host)
            {
                host.Start();

                host.WaitForShutdown(token, shutdownMessage);
            }
        }

        private static void WaitForShutdown(this IWebHost host, CancellationToken token, string shutdownMessage)
        {
            var hostingEnvironment = host.Services.GetService<IHostingEnvironment>();
            var applicationLifetime = host.Services.GetService<IApplicationLifetime>();

            Console.WriteLine($"Hosting environment: {hostingEnvironment.EnvironmentName}");
            Console.WriteLine($"Content root path: {hostingEnvironment.ContentRootPath}");

            var serverAddresses = host.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;
            if (serverAddresses != null)
            {
                foreach (var address in serverAddresses)
                {
                    Console.WriteLine($"Now listening on: {address}");
                }
            }

            if (!string.IsNullOrEmpty(shutdownMessage))
            {
                Console.WriteLine(shutdownMessage);
            }

            token.Register(state =>
            {
                ((IApplicationLifetime)state).StopApplication();
            },
            applicationLifetime);

            applicationLifetime.ApplicationStopping.WaitHandle.WaitOne();
        }

        private static void WaitForSystemShutdown(this IWebHost host, Action<CancellationToken, string> execute)
        {
            var done = new ManualResetEventSlim(false);
            using (var cts = new CancellationTokenSource())
            {
                Action shutdown = () =>
                {
                    if (!cts.IsCancellationRequested)
                    {
                        Console.WriteLine("Application is shutting down...");
                        cts.Cancel();
                    }

                    done.Wait();
                };

#if NETSTANDARD1_5
                var assemblyLoadContext = AssemblyLoadContext.GetLoadContext(typeof(WebHostExtensions).GetTypeInfo().Assembly);
                assemblyLoadContext.Unloading += context => shutdown();
#elif NETSTANDARD1_3
#else
#error Target frameworks need to be updated.                
#endif
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    shutdown();
                    // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                    eventArgs.Cancel = true;
                };

                execute(cts.Token, "Application started. Press Ctrl+C to shut down.");
                done.Set();
            }
        }
    }
}