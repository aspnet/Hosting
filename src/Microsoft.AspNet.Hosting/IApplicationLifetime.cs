using System.Threading;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Hosting
{
    [AssemblyNeutral]
    public interface IApplicationLifetime
    {
        CancellationToken OnApplicationShutdown { get; }
    }
}
