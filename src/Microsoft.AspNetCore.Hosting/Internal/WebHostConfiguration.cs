// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class WebHostConfiguration
    {
        public static IConfiguration GetDefault()
        {
            return GetDefault(args: null);
        }

        public static IConfiguration GetDefault(string[] args)
        {
            // Setup the default locations for finding hosting configuration options
            // hosting.json, ASPNETCORE_ prefixed env variables and command line arguments
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile(WebHostDefaults.HostingJsonFile, optional: true);

            if (args != null)
            {
                configBuilder.AddCommandLine(args);
            }

            return configBuilder.Build();
        }
    }

}
