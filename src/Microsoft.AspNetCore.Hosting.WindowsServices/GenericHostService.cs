// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ServiceProcess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting.WindowsServices
{
    /// <summary>
    ///     Provides an implementation of a Windows service that hosts ASP.NET Core.
    /// </summary>
    public class GenericHostService : ServiceBase
    {
        private IHost _host;
        private bool _stopRequestedByWindows;

        /// <summary>
        /// Creates an instance of <c>GenericHostService</c> which hosts the specified application.
        /// </summary>
        /// <param name="host">The configured generic host containing the application to run in the Windows service.</param>
        public GenericHostService(IHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        protected sealed override void OnStart(string[] args)
        {
            OnStarting(args);

            _host
                .Services
                .GetRequiredService<IApplicationLifetime>()
                .ApplicationStopped
                .Register(() =>
                {
                    if (!_stopRequestedByWindows)
                    {
                        Stop();
                    }
                });

            _host.Start();

            OnStarted();
        }

        protected sealed override void OnStop()
        {
            _stopRequestedByWindows = true;
            OnStopping();
            try
            {
                _host.StopAsync().GetAwaiter().GetResult();
            }
            finally
            {
                _host.Dispose();
                OnStopped();
            }
        }

        /// <summary>
        /// Executes before ASP.NET Core starts.
        /// </summary>
        /// <param name="args">The command line arguments passed to the service.</param>
        protected virtual void OnStarting(string[] args) { }

        /// <summary>
        /// Executes after ASP.NET Core starts.
        /// </summary>
        protected virtual void OnStarted() { }

        /// <summary>
        /// Executes before ASP.NET Core shuts down.
        /// </summary>
        protected virtual void OnStopping() { }

        /// <summary>
        /// Executes after ASP.NET Core shuts down.
        /// </summary>
        protected virtual void OnStopped() { }
    }
}
