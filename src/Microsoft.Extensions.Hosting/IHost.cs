using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Represents a configured web host.
    /// </summary>
    public interface IHost : IDisposable
    {
        /// <summary>
        /// Triggered when the application requests shutdown
        /// </summary>
        CancellationToken ShutdownRequested { get; }

        /// <summary>
        /// The <see cref="IServiceProvider"/> for the host.
        /// </summary>
        IServiceProvider Services { get; }

        /// <summary>
        /// Starts listening on the configured addresses.
        /// </summary>
        void Start();
    }
}
