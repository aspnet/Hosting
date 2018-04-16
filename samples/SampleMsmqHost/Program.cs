using System.Messaging;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SampleMsmqHost
{
    public static class Program
    {
        // Before running this program, please make sure to install MSMQ
        // and create the ".\private$\SampleQueue" queue on your local machine.

        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddEnvironmentVariables();
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddCommandLine(args);
                })
                .ConfigureLogging(factory =>
                {
                    factory.AddConsole();
                })
                .ConfigureServices(services =>
                {
                    services.AddOptions();

                    services.Configure<MsmqOptions>(options =>
                    {
                        options.Path = @".\private$\SampleQueue";
                        options.AccessMode = QueueAccessMode.SendAndReceive;
                    });

                    services.AddSingleton<IMsmqConnection, MsmqConnection>();
                    services.AddTransient<IMsmqProcessor, MsmqProcessor>();
                    services.AddTransient<IHostedService, MsmqService>();
                    services.AddTransient<IHostedService, ConsoleInputService>();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }

    }
}