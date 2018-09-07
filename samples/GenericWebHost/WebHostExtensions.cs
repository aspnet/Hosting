using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;

namespace GenericWebHost
{
    public static class WebHostExtensions
    {
        public static IHostBuilder ConfigureWebHost(this IHostBuilder builder, Action<HostBuilderContext, IApplicationBuilder> configure)
        {
            return builder.ConfigureServices((bulderContext, services) =>
            {
                services.Configure<WebHostServiceOptions>(options =>
                {
                    options.Configure = configure;
                });
                services.AddHostedService<WebHostService>();
                
                var listener = new DiagnosticListener("Microsoft.AspNetCore");
                services.AddSingleton<DiagnosticListener>(listener);
                services.AddSingleton<DiagnosticSource>(listener);
                
                services.AddTransient<IHttpContextFactory, HttpContextFactory>();
                services.AddScoped<IMiddlewareFactory, MiddlewareFactory>();

                // Conjure up a RequestServices
                services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();
                services.AddTransient<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();

                // Ensure object pooling is available everywhere.
                services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            });
        }
    }
}
