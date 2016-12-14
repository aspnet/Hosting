using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;

namespace Microsoft.Extensions.Hosting
{
    public class Host : IHost
    {
        private readonly HostedServiceExecutor _executor;
        private readonly CancellationTokenSource _cts;

        public Host(IServiceProvider services, CancellationTokenSource cts)
        {
            Services = services;
            _cts = cts;
            ShutdownRequested = cts.Token;
            _executor = services.GetRequiredService<HostedServiceExecutor>();
        }

        public IServiceProvider Services { get; }

        public CancellationToken ShutdownRequested { get; }

        public void Start()
        {
            _executor.Start();
        }

        public void Dispose()
        {
            _executor.Stop();

            // TODO: Catch exceptions
            _cts.Cancel(throwOnFirstException: false);

            (Services as IDisposable)?.Dispose();
        }
    }
}
