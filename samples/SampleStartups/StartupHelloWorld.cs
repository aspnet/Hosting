using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

// Note that this sample will not run. It is only here to illustrate usage patterns.

namespace SampleStartups
{
    public class Program
    {
        public static void Main()
        {
            var host = new WebHostBuilder()
                .Run(async context =>
                {
                    await context.Response.WriteAsync("Hello World");
                });

            host.WaitForShutdown();
        }

        public static void MainWithPort()
        {
            var host = new WebHostBuilder()
                .Run(8080, async context =>
                {
                    await context.Response.WriteAsync("Hello World");
                });

            host.WaitForShutdown();
        }

        public static void MainWithMiddlewareWithPort()
        {
            var host = new WebHostBuilder()
                .RunApplication(8080, app =>
                {
                    // You can add middleware here
                    app.Run(async context =>
                        {
                            await context.Response.WriteAsync("Hello World");
                        });
                });

            host.WaitForShutdown();
        }

        public static void MainWithMiddleware()
        {
            var host = new WebHostBuilder()
                .RunApplication(app =>
                {
                    // You can add middleware here
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Hello World");
                    });
                });

            host.WaitForShutdown();
        }
    }
}
