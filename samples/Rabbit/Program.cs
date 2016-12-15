using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Rabbit
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureLogging(l => l.AddConsole())
                //.UseWebApplication(builder => builder.Configure(app =>
                //{
                //    // app.UseEndPoint<MyEndPoint>("/myep");
                //    // app.UseMvc();
                //}))
                //.UseTcpListener(5050, connection =>
                //{
                //    // Do something with the connection
                //    return Task.CompletedTask;
                //})
                //.UseEndPoint<MyEndPoint>(5051)
                .ConfigureServices(s => s.AddSimpleBackgroundQueue(new QueueOptions() { HostName = "localhost", QueueName="task_queue" }))
                .UseSimpleBackgroundQueueWorker<MyWorker>()
                .Build();

            host.Run();
        }
    }

    public interface ISimpleBackgroundQueue
    {
        QueueOptions QueueOptions { get; set; }
        IConnectionFactory ConnectionFactory { get; set; }
    }

    public class SimpleBackgroundQueueService : ISimpleBackgroundQueue
    {
        public QueueOptions QueueOptions { get; set; }

        public IConnectionFactory ConnectionFactory { get; set; }

        public SimpleBackgroundQueueService(QueueOptions options)
        {
            QueueOptions = options;
            ConnectionFactory = new ConnectionFactory()
            {
                HostName = options.HostName
            };
        }
    }


    public static class HostBuilderExtensions
    {
        public static HostBuilder UseEndPoint<T>(this HostBuilder self, int port) // where T: EndPoint
        {
            return self;
        }

        public static HostBuilder UseSimpleBackgroundQueueWorker<T>(this HostBuilder self) where T : QueueHandler
        {
            self.ConfigureServices(s =>
            {
                s.AddSingleton<IHostedService, RabbitMQHostedService<T>>();
            });
            return self;
        }

        //public static HostBuilder UseTcpListener(this HostBuilder self, int port, Func<Socket, Task> body)
        //{
        //    return self.UseWorker(async cancellationToken =>
        //    {
        //        var listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
        //        var tasks = new List<Task>();
        //        while(!cancellationToken.IsCancellationRequested)
        //        {
        //            var socket = await listener.AcceptSocketAsync();

        //            // In theory we could just not track the tasks... it depends on how much we care
        //            // The list will never stop growing though, until the service is shut down. But the
        //            // tasks within it will complete at their own pace.
        //            tasks.Add(body(socket));
        //        }

        //        // Wait for all remaining outstanding tasks to complete.
        //        await Task.WhenAll(tasks);
        //    });
        //}

        //public static HostBuilder UseHostedService(this HostBuilder self, IHostedService service)
        //{
        //    return self;
        //}

        //public static HostBuilder UseWorker(this HostBuilder self, Func<CancellationToken, Task> body)
        //{
        //    return self;
        //}
    }
}
