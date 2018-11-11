using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal interface ISupportsStartup
    {
        IWebHostBuilder Configure(Action<IApplicationBuilder> configure);
        IWebHostBuilder UseStartup(Type startupType);
    }
}
