// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if NETSTANDARD1_5
using System.Reflection;
using System.Runtime.Loader;
#endif
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting
{
    public static class WebHostExtensions
    {
        /// <summary>
        /// Runs a web application and block the calling thread until host shutdown.
        /// </summary>
        /// <param name="host">The <see cref="IHost"/> to run.</param>
        public static void Run(this IHost host)
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
#endif
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    shutdown();
                    // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                    eventArgs.Cancel = true;
                };

                host.Run(cts.Token, "Application started. Press Ctrl+C to shut down.");
                done.Set();
            }
        }

        /// <summary>
        /// Runs a web application and block the calling thread until token is triggered or shutdown is triggered.
        /// </summary>
        /// <param name="host">The <see cref="IHost"/> to run.</param>
        /// <param name="token">The token to trigger shutdown.</param>
        public static void Run(this IHost host, CancellationToken token)
        {
            host.Run(token, shutdownMessage: null);
        }

        private static void Run(this IHost host, CancellationToken token, string shutdownMessage)
        {
            using (host)
            {
                host.Start();

                var control = host.Services.GetService<IHostLifetimeControl>();

                if (!string.IsNullOrEmpty(shutdownMessage))
                {
                    Console.WriteLine(shutdownMessage);
                }

                token.Register(state =>
                {
                    ((IHostLifetimeControl)state).Shutdown();
                },
                control);

                host.ShutdownRequested.WaitHandle.WaitOne();
            }
        }
    }
}