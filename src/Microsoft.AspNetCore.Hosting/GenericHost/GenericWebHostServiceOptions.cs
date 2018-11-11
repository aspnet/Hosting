using System;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal class GenericWebHostServiceOptions
    {
        public Action<IApplicationBuilder> ConfigureApplication { get; set; } = DefaultApplication;

        public WebHostOptions WebHostOptions { get; set; }

        public AggregateException HostingStartupExceptions { get; set; }

        private static Action<IApplicationBuilder> DefaultApplication => _ => throw new InvalidOperationException($"No application configured. Please specify an application via IWebHostBuilder.UseStartup, IWebHostBuilder.Configure, or specifying the startup assembly via {nameof(WebHostDefaults.StartupAssemblyKey)} in the web host configuration.");
    }
}
