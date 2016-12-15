// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class WebHostOptions
    {
        public WebHostOptions()
        {
        }

        public WebHostOptions(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            ApplicationName = configuration[WebHostDefaults.ApplicationKey];
            StartupAssembly = configuration[WebHostDefaults.StartupAssemblyKey];
            DetailedErrors = ParseBool(configuration, WebHostDefaults.DetailedErrorsKey);
            CaptureStartupErrors = ParseBool(configuration, WebHostDefaults.CaptureStartupErrorsKey);
            Environment = configuration[WebHostDefaults.EnvironmentKey];
            WebRoot = configuration[WebHostDefaults.WebRootKey];
            ContentRootPath = configuration[WebHostDefaults.ContentRootKey];
        }

        public WebHostOptions(HostBuilder builder)
        {
            ApplicationName = builder.GetSetting(WebHostDefaults.ApplicationKey);
            StartupAssembly = builder.GetSetting(WebHostDefaults.StartupAssemblyKey);
            DetailedErrors = ParseBool(builder, WebHostDefaults.DetailedErrorsKey);
            CaptureStartupErrors = ParseBool(builder, WebHostDefaults.CaptureStartupErrorsKey);
            Environment = builder.GetSetting(WebHostDefaults.EnvironmentKey);
            WebRoot = builder.GetSetting(WebHostDefaults.WebRootKey);
            ContentRootPath = builder.GetSetting(WebHostDefaults.ContentRootKey);
        }

        public string ApplicationName { get; set; }

        public bool DetailedErrors { get; set; }

        public bool CaptureStartupErrors { get; set; }

        public string Environment { get; set; }

        public string StartupAssembly { get; set; }

        public string WebRoot { get; set; }

        public string ContentRootPath { get; set; }

        private static bool ParseBool(IConfiguration configuration, string key)
        {
            return string.Equals("true", configuration[key], StringComparison.OrdinalIgnoreCase)
                || string.Equals("1", configuration[key], StringComparison.OrdinalIgnoreCase);
        }

        private static bool ParseBool(HostBuilder configuration, string key)
        {
            return string.Equals("true", configuration.GetSetting(key), StringComparison.OrdinalIgnoreCase)
                || string.Equals("1", configuration.GetSetting(key), StringComparison.OrdinalIgnoreCase);
        }
    }
}