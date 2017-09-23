// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Base class for implementing a long running <see cref="IHostedService"/>.
    /// </summary>
    public abstract class BackgroundService : IHostedService, IDisposable
    {
        private static readonly Func<object, Task> _executeBackgroundTask = ExecuteBackgroundTaskAsync;

        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        // For testing purposes only
        internal Task ExecutingTask => _executingTask;

        /// <summary>
        /// This method is called when the <see cref="IHostedService"/> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="IHostedService.StopAsync(CancellationToken)"/> is called.</param>
        /// <returns>A <see cref="Task"/> that represents the long running operations.</returns>
        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            // Store the task we're executing
            _executingTask = Task.Factory.StartNew(_executeBackgroundTask,
                                                   this,
                                                   cancellationToken,
                                                   TaskCreationOptions.DenyChildAttach,
                                                   TaskScheduler.Default).Unwrap();
            // Otherwise it's running
            return Task.CompletedTask;
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                var task = await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));

                // Bubble any exceptions thrown by the executing task
                if (task == _executingTask)
                {
                    try
                    {
                        await _executingTask;
                    }
                    catch (TaskCanceledException)
                    {
                        // Swallow task canclled exceptions since it might be by design that
                        // the executing task is cancelled
                    }
                }
            }
        }

        public virtual void Dispose()
        {
            _stoppingCts.Cancel();
        }

        private static Task ExecuteBackgroundTaskAsync(object state)
        {
            var service = (BackgroundService)state;

            return service.ExecuteAsync(service._stoppingCts.Token);
        }
    }
}
