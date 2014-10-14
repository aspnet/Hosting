// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Fakes
{
    public class StartupRequest
    {
        public StartupRequest()
        {
        }

        public void ConfigureRequestServices(IServiceCollection services)
        {
            services.AddScoped<IFakeService, FakeService>();
        }

        public void Configure(IApplicationBuilder builder)
        {
            //builder.UseRequestServices();
        }
    }
}