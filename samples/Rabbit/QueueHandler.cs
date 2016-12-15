using RabbitMQ.Client.Events;
using System.Threading.Tasks;

namespace Rabbit
{
    public abstract class QueueHandler
    {
        public abstract Task HandleMessage(BasicDeliverEventArgs message);
    }
}