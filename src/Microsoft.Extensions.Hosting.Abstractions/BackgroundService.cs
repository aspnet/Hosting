﻿// Copyright (c) .NET Foundation. All rights reserved.
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
        private static Action<object> _cancelToken = CancelToken;

        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        private readonly CancellationTokenSource _cancelledCts = new CancellationTokenSource();

        public BackgroundService()
        {
            CancelledToken = _cancelledCts.Token;
        }

        /// <summary>
        /// This method is called when the <see cref="IHostedService"/> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="IHostedService.StopAsync(CancellationToken)"/> is called.</param>
        /// <returns>A <see cref="Task"/> that represents the long running operations.</returns>
        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Triggered if cancellation is triggered during <see cref="IHostedService.StartAsync(CancellationToken)"/> or <see cref="IHostedService.StopAsync(CancellationToken)"/>.
        /// </summary>
        public CancellationToken CancelledToken { get; }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(_cancelToken, _cancelledCts))
            {
                // Store the task we're executing
                _executingTask = ExecuteAsync(_stoppingCts.Token);

                // If the task is completed then return it, this will bubble cancellation and failure to the caller
                if (_executingTask.IsCompleted)
                {
                    return _executingTask;
                }

                // Otherwise it's running
                return Task.CompletedTask;
            }
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            using (cancellationToken.Register(_cancelToken, _cancelledCts))
            {
                try
                {
                    // Signal cancellation to the executing method
                    _stoppingCts.Cancel();
                }
                finally
                {
                    // Wait until the task completes or the stop token triggers
                    await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));
                }
            }
        }

        public virtual void Dispose()
        {
            _stoppingCts.Cancel();
            _cancelledCts.Cancel();

            _stoppingCts.Dispose();
            _cancelledCts.Dispose();
        }

        private static void CancelToken(object state) => ((CancellationTokenSource)state).Cancel();
    }
}
