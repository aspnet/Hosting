using System;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal class GenericWebHostServiceOptions
    {
        public Action<IApplicationBuilder> ConfigureApplication { get; set; }

        public WebHostOptions WebHostOptions { get; set; }

        public AggregateException HostingStartupExceptions { get; set; }
    }
}
