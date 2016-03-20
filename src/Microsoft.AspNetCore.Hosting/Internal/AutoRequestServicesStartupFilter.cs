// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class AutoRequestServicesStartupFilter : IStartupFilter
    {
        public ConfigureDelegate Configure(ConfigureDelegate next)
        {
            return builder =>
            {
                builder.UseMiddleware<RequestServicesContainerMiddleware>();
                next(builder);
            };
        }
    }
}
