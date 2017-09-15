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
        private static Action<object> _cancelToken = CancelToken;

        private Task _executingTask;
        private CancellationTokenSource _executionCts;
        private readonly CancellationTokenSource _shutdownCts = new CancellationTokenSource();

        /// <summary>
        /// This method is called when the <see cref="IHostedService"/> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that fires when the <see cref="IHostedService"/> is stopped.</param>
        /// <returns>A <see cref="Task"/> that represents the long running operations.</returns>
        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);

        /// <summary>
        /// A <see cref="CancellationToken"/> that repesents the ungraceful shutdown.
        /// </summary>
        public CancellationToken ShutdownToken => _shutdownCts.Token;

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(_cancelToken, _shutdownCts))
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

            using (cancellationToken.Register(_cancelToken, _shutdownCts))
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
            _shutdownCts.Dispose();
            _executionCts?.Dispose();
        }

        private static void CancelToken(object state) => ((CancellationTokenSource)state).Cancel();
    }
}
