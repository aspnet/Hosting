// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public static class HostingEventSourceServiceCollectionExtensions
    {
        public static IServiceCollection AddHostingEventSource(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<HostingEventSourceListener>();
            serviceCollection.AddSingleton<IStartupFilter, HostingEventSourceStartupFilter>();

            return serviceCollection;
        }
    }
}
