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
    public abstract class BackgroundService : IHostedService, IDisposable
    {
        private Task _executingTask;
        private CancellationTokenSource _executionCts;
        private readonly CancellationTokenSource _startCts = new CancellationTokenSource();
        private readonly CancellationTokenSource _stopCts = new CancellationTokenSource();

        /// <summary>
        /// This method is called when the <see cref="IHostedService"/> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that fires when the <see cref="IHostedService"/> is stopped.</param>
        /// <returns>A <see cref="Task"/> that represents the long running operations.</returns>
        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);

        /// <summary>
        /// A <see cref="CancellationToken"/> that represents the <see cref="IHostedService.StartAsync(CancellationToken)"/> call.
        /// </summary>
        public CancellationToken StartToken => _startCts.Token;

        /// <summary>
        /// A <see cref="CancellationToken"/> that repesents the <see cref="IHostedService.StopAsync(CancellationToken)"/> call.
        /// </summary>
        public CancellationToken StopToken => _stopCts.Token;

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(_startCts.Cancel))
            {
                // This token
                _executionCts = new CancellationTokenSource();

                // Store the task we're executing
                _executingTask = ExecuteAsync(_executionCts.Token);

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

            using (cancellationToken.Register(_stopCts.Cancel))
            {
                try
                {
                    // Signal cancellation to the executing method
                    _executionCts.Cancel();
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
            _startCts.Dispose();
            _stopCts.Dispose();
            _executionCts?.Dispose();
        }
    }
}
