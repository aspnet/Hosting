using System.Threading;

namespace Microsoft.AspNet.Hosting
{
    public class ManualApplicationLifetime : IApplicationLifetime
    {
        private CancellationTokenSource _shutdownSource = new CancellationTokenSource();

        public CancellationToken OnApplicationShutdown
        {
            get { return _shutdownSource.Token; }
        }

        public void Shutdown()
        {
            _shutdownSource.Cancel();
        }
    }
}
