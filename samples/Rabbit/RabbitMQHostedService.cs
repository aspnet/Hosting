using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rabbit
{
    public class RabbitMQHostedService<T> : IHostedService where T : QueueHandler
    {
        private IConnection _connection;
        private IModel _channel;
        private IServiceProvider _provider;
        private ILogger<RabbitMQHostedService<T>> _logger;
        private EventingBasicConsumer _consumerDispatcher;
        private ISimpleBackgroundQueue _queue;

        public RabbitMQHostedService(IServiceProvider provider, ILogger<RabbitMQHostedService<T>> logger, ISimpleBackgroundQueue queue)
        {
            _provider = provider;
            _logger = logger;
            _queue = queue;
        }

        public void Start()
        {
            
            _connection = _queue.ConnectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: _queue.QueueOptions.QueueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            //Don't do RoundRobin. Only dispatch if worker not busy.
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation(" [*] Waiting for messages.");

            _consumerDispatcher = new EventingBasicConsumer(_channel);

            _consumerDispatcher.Received += (model, ea) =>
            {
                DispatchToConsumer(ea);
            };
            _channel.BasicConsume(queue: _queue.QueueOptions.QueueName,
                                  consumer: _consumerDispatcher);
        }

        private void DispatchToConsumer(BasicDeliverEventArgs ea)
        {
            using (var messageProvider = _provider.CreateScope())
            {
                //TODO: Make sure we always turn on correlation Ids. Part of our opinion of the stack.
                _logger.LogDebug($"Received message {0}", ea.BasicProperties.CorrelationId);

                var consumer = ActivatorUtilities.CreateInstance<T>(messageProvider.ServiceProvider);
                consumer.HandleMessage(ea);

                _logger.LogDebug($"Message Complete {0}", ea.BasicProperties.CorrelationId);

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        }

        public void Stop()
        {
            _channel.Dispose();
            _connection.Dispose();
        }
    }
}
