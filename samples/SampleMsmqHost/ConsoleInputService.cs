using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace SampleMsmqHost
{
    public class ConsoleInputService : IHostedService, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public IMsmqConnection Connection { get; }

        public ConsoleInputService(IMsmqConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var stoppingToken = _cancellationTokenSource.Token;

            // run the read loop in a background thread
            Task.Run(() => ReadLoopAsync(stoppingToken), cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // tell the read loop to stop
            _cancellationTokenSource.Cancel();

            return Task.CompletedTask;
        }

        private async Task ReadLoopAsync(CancellationToken cancellationToken)
        {
            await Console.Out.WriteLineAsync("Enter your text message and press ENTER...");

            while (!cancellationToken.IsCancellationRequested)
            {
                // read a text message from the user
                cancellationToken.ThrowIfCancellationRequested();
                var text = await Console.In.ReadLineAsync();

                // send the text message to the queue
                cancellationToken.ThrowIfCancellationRequested();
                if (!string.IsNullOrEmpty(text))
                    await Connection.SendTextAsync(text, cancellationToken);
            }
        }

    }
}