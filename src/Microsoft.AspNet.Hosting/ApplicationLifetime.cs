// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Hosting
{
    /// <summary>
    /// Allows consumers to perform cleanup during a graceful shutdown.
    /// </summary>
    public class ApplicationLifetime : IApplicationLifetime
    {
        private readonly IApplicationShutdown _applicationShutdown;
        private readonly CancellationTokenSource _startedSource = new CancellationTokenSource();
        private readonly CancellationTokenSource _stoppingSource = new CancellationTokenSource();
        private readonly CancellationTokenSource _stoppedSource = new CancellationTokenSource();

        public ApplicationLifetime(IApplicationShutdown applicationShutdown)
        {
            _applicationShutdown = applicationShutdown;
        }

        /// <summary>
        /// Triggered when the application host has fully started and is about to wait
        /// for a graceful shutdown.
        /// </summary>
        public CancellationToken ApplicationStarted
        {
            get { return _startedSource.Token; }
        }

        /// <summary>
        /// Terminates the application
        /// </summary>
        public void StopApplication() => _applicationShutdown.RequestShutdown();

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// Request may still be in flight. Shutdown will block until this event completes.
        /// </summary>
        /// <returns></returns>
        public CancellationToken ApplicationStopping
        {
            get { return _stoppingSource.Token; }
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// All requests should be complete at this point. Shutdown will block
        /// until this event completes.
        /// </summary>
        /// <returns></returns>
        public CancellationToken ApplicationStopped
        {
            get { return _stoppedSource.Token; }
        }

        /// <summary>
        /// Signals the ApplicationStarted event and blocks until it completes.
        /// </summary>
        public void NotifyStarted()
        {
            try
            {
                _startedSource.Cancel(throwOnFirstException: false);
            }
            catch (Exception)
            {
                // TODO: LOG
            }
        }

        /// <summary>
        /// Signals the ApplicationStopping event and blocks until it completes.
        /// </summary>
        public void NotifyStopping()
        {
            try
            {
                _stoppingSource.Cancel(throwOnFirstException: false);
            }
            catch (Exception)
            {
                // TODO: LOG
            }
        }

        /// <summary>
        /// Signals the ApplicationStopped event and blocks until it completes.
        /// </summary>
        public void NotifyStopped()
        {
            try
            {
                _stoppedSource.Cancel(throwOnFirstException: false);
            }
            catch (Exception)
            {
                // TODO: LOG
            }
        }
    }
}
