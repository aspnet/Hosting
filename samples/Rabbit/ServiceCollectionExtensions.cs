using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rabbit
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSimpleBackgroundQueue(this IServiceCollection self, QueueOptions options)
        {
            //TODO: Options and config...
            self.AddSingleton<ISimpleBackgroundQueue>(new SimpleBackgroundQueueService(options));
            return self;
        }
    }
}
