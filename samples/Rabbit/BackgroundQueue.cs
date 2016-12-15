using System;

namespace Rabbit
{
    public class BackgroundQueue
    {
        private QueueOptions options;

        public BackgroundQueue(QueueOptions options)
        {
            this.options = options;
        }
    }
}