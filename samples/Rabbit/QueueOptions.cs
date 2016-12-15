namespace Rabbit
{
    public class QueueOptions
    {
        public string HostName { get; set; }
        public int? Port { get; set; }
        public string QueueName { get; internal set; }
    }
}