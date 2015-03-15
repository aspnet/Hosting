// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Startup;

namespace Microsoft.AspNet.Hosting.Internal
{
    public class AutoRequestServicesStartupFilter : IStartupFilter
    {
        public void Configure(IApplicationBuilder app, Action<IApplicationBuilder> next)
        {
            app.UseMiddleware<RequestServicesContainerMiddleware>();
            next(app);
        }
    }
}
