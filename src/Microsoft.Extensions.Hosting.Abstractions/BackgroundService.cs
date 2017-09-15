using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Base class for implementing a long running <see cref="IHostedService"/>.
    /// </summary>
    public abstract class BackgroundService : IHostedService
    {
        private Task _executingTask;
        private CancellationTokenSource _cts;

        /// <summary>
        /// This method is called when the <see cref="IHostedService"/> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that fires when the <see cref="IHostedService"/> is stopped or cancelled.</param>
        /// <returns>A <see cref="Task"/> that represents the long running operations.</returns>
        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // If the token is cancellable, create a linked token so we can trigger cancellation 
            // on shutdown and based on the incoming token
            _cts = cancellationToken.CanBeCanceled ?
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken) :
                new CancellationTokenSource();

            // Store the task we're executing
            _executingTask = ExecuteAsync(_cts.Token);

            // If the task is completed then return it, this will bubble cancellation and failure to the caller
            if (_executingTask.IsFaulted)
            {
                return _executingTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                _cts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));
            }
        }
    }
}
