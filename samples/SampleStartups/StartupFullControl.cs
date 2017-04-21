using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Note that this sample will not run. It is only here to illustrate usage patterns.

namespace SampleStartups
{
    public class StartupFullControl
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .AddJsonFile("hosting.json", optional: true)
                .Build();

            var host = new WebHostBuilder()
                .UseConfiguration(config) // Default set of configurations to use, may be subsequently overridden 
                //.UseKestrel()
                .UseFakeServer()
                .UseContentRoot(Directory.GetCurrentDirectory()) // Override the content root with the current directory
                .UseUrls("http://*:1000", "https://*:902")
                .UseEnvironment("Development")
                .UseWebRoot("public")
                .ConfigureServices(services =>
                {
                    // Configure services that the application can see
                    services.AddSingleton<IMyCustomService, MyCustomService>();
                })
                .Configure(app =>
                {
                    // Write the application inline, this won't call any startup class in the assembly

                    app.Use(next => context =>
                    {
                        return next(context);
                    });
                })
                .Build();

            host.Run();
        }
    }

    public class MyHostLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public interface IMyCustomService
    {
        void Go();
    }

    public class MyCustomService : IMyCustomService
    {
        public void Go()
        {
            throw new NotImplementedException();
        }
    }
}
