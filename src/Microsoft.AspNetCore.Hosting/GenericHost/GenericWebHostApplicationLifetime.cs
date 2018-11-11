using System.Threading;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal class GenericWebHostApplicationLifetime : IApplicationLifetime
    {
        private readonly Microsoft.Extensions.Hosting.IApplicationLifetime _applicationLifetime;
        public GenericWebHostApplicationLifetime(Microsoft.Extensions.Hosting.IApplicationLifetime applicationLifetime)
        {
            _applicationLifetime = applicationLifetime;
        }

        public CancellationToken ApplicationStarted => _applicationLifetime.ApplicationStarted;

        public CancellationToken ApplicationStopping => _applicationLifetime.ApplicationStopping;

        public CancellationToken ApplicationStopped => _applicationLifetime.ApplicationStopped;

        public void StopApplication() => _applicationLifetime.StopApplication();
    }
}
