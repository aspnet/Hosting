using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace GenericHostSample
{
    public class MyServiceA : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("MyServiceA is starting.");

            cancellationToken.Register(() => Console.WriteLine("MyServiceA is stopping."));

            while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("MyServiceA is doing background work.");

                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            Console.WriteLine("MyServiceA background task is stopping.");
        }
    }
}
