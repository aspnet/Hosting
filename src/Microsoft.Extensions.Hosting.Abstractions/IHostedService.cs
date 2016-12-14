using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Defines methods for objects that are managed by the host.
    /// </summary>
    public interface IHostedService
    {
        /// <summary>
        /// Triggered when the application host has fully started.
        /// </summary>
        void Start();

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        void Stop();
    }
}
