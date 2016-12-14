using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHostLifetimeControl
    {
        /// <summary>
        /// Requests termination the current application.
        /// </summary>
        void Shutdown();
    }
}
