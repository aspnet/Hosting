using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;

namespace Microsoft.Extensions.Hosting
{
    public static class GenericHostWebHostBuilderExtensions
    {
        public static IHostBuilder ConfigureWebHost(this IHostBuilder builder, Action<IWebHostBuilder> configure)
        {
            var webhostBuilder = new GenericWebHostBuilder(builder, allowBuild: false);
            configure(webhostBuilder);
            return builder;
        }
    }
}
