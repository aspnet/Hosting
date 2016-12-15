using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using System;
using System.Threading.Tasks;

namespace Rabbit
{
    public class MyWorker : QueueHandler
    {
        private ILogger<MyWorker> _logger;

        public MyWorker(ILogger<MyWorker> logger)
        {
            _logger = logger;
        }
        public override Task HandleMessage(BasicDeliverEventArgs message)
        {
            _logger.LogInformation($"processing message {message.BasicProperties.CorrelationId}");
            return Task.CompletedTask;
        }
    }
}