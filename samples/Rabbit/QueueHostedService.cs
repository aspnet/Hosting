using System;
using Microsoft.Extensions.Hosting;

namespace Rabbit
{
    internal class QueueHostedService<T> : IHostedService where T : QueueHandler
    {
        private string queueName;

        public QueueHostedService(string queueName)
        {
            this.queueName = queueName;
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}