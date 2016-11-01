// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    /// <summary>
    /// Allows consumers to perform cleanup during a graceful shutdown.
    /// </summary>
    public class ApplicationLifetime : IApplicationLifetime
    {
        private readonly CancellationTokenSource _startedSource = new CancellationTokenSource();
        private readonly CancellationTokenSource _stoppingSource = new CancellationTokenSource();
        private readonly CancellationTokenSource _stoppedSource = new CancellationTokenSource();
        private readonly IEnumerable<IApplicationLifetimeEvents> _handlers = Enumerable.Empty<IApplicationLifetimeEvents>();
        private readonly ILogger<ApplicationLifetime> _logger;

        public ApplicationLifetime(ILogger<ApplicationLifetime> logger)
        {
            _logger = logger;
        }

        public ApplicationLifetime(ILogger<ApplicationLifetime> logger, IEnumerable<IApplicationLifetimeEvents> handlers)
        {
            _logger = logger;
            _handlers = handlers;
        }

        /// <summary>
        /// Triggered when the application host has fully started and is about to wait
        /// for a graceful shutdown.
        /// </summary>
        public CancellationToken ApplicationStarted => _startedSource.Token;

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// Request may still be in flight. Shutdown will block until this event completes.
        /// </summary>
        public CancellationToken ApplicationStopping => _stoppingSource.Token;

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// All requests should be complete at this point. Shutdown will block
        /// until this event completes.
        /// </summary>
        public CancellationToken ApplicationStopped => _stoppedSource.Token;

        /// <summary>
        /// Signals the ApplicationStopping event and blocks until it completes.
        /// </summary>
        public void StopApplication()
        {
            // Lock on CTS to synchronize multiple calls to StopApplication. This guarantees that the first call 
            // to StopApplication and its callbacks run to completion before subsequent calls to StopApplication, 
            // which will no-op since the first call already requested cancellation, get a chance to execute.
            lock (_stoppingSource)
            {
                // Noop if this is already cancelled, user code can call this so 
                // we should guard against multiple calls here
                if (_stoppingSource.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    foreach (var handler in _handlers)
                    {
                        handler.OnApplicationStopping();
                    }

                    _stoppingSource.Cancel(throwOnFirstException: false);
                }
                catch (Exception ex)
                {
                    _logger.ApplicationError(LoggerEventIds.ApplicationStoppingException,
                                             "An error occurred stopping the application",
                                             ex);
                }
            }
        }

        /// <summary>
        /// Signals the ApplicationStarted event and blocks until it completes.
        /// </summary>
        public void NotifyStarted()
        {
            try
            {
                foreach (var handler in _handlers)
                {
                    handler.OnApplicationStarted();
                }

                _startedSource.Cancel(throwOnFirstException: false);
            }
            catch (Exception ex)
            {
                _logger.ApplicationError(LoggerEventIds.ApplicationStartupException,
                                         "An error occurred starting the application",
                                         ex);
            }
        }

        /// <summary>
        /// Signals the ApplicationStopped event and blocks until it completes.
        /// </summary>
        public void NotifyStopped()
        {
            try
            {
                foreach (var handler in _handlers)
                {
                    handler.OnApplicationStopped();
                }

                _stoppedSource.Cancel(throwOnFirstException: false);
            }
            catch (Exception ex)
            {
                _logger.ApplicationError(LoggerEventIds.ApplicationStoppedException,
                                         "An error occurred stopping the application",
                                         ex);
            }
        }
    }
}
