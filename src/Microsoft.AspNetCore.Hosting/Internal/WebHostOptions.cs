// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class WebHostOptions
    {
        public WebHostOptions() { }

        public WebHostOptions(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var applicationName = configuration[WebHostDefaults.ApplicationKey];
            var startupAssembly = configuration[WebHostDefaults.StartupAssemblyKey];

            if (string.IsNullOrEmpty(applicationName))
            {
                // Fall back to name of Startup assembly if
                // application name hasn't been overridden.
                applicationName = startupAssembly;
            }

            ApplicationName = applicationName;
            StartupAssembly = startupAssembly;
            FindStartupType = WebHostUtilities.ParseBool(configuration, WebHostDefaults.FindStartupTypeKey);
            DetailedErrors = WebHostUtilities.ParseBool(configuration, WebHostDefaults.DetailedErrorsKey);
            CaptureStartupErrors = WebHostUtilities.ParseBool(configuration, WebHostDefaults.CaptureStartupErrorsKey);
            Environment = configuration[WebHostDefaults.EnvironmentKey];
            WebRoot = configuration[WebHostDefaults.WebRootKey];
            ContentRootPath = configuration[WebHostDefaults.ContentRootKey];
            PreventHostingStartup = WebHostUtilities.ParseBool(configuration, WebHostDefaults.PreventHostingStartupKey);
            // Search the primary assembly and configured assemblies.
            HostingStartupAssemblies = $"{StartupAssembly};{configuration[WebHostDefaults.HostingStartupAssembliesKey]}"
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];

            var timeout = configuration[WebHostDefaults.ShutdownTimeoutKey];
            if (!string.IsNullOrEmpty(timeout)
                && int.TryParse(timeout, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
            {
                ShutdownTimeout = TimeSpan.FromSeconds(seconds);
            }
        }

        public string ApplicationName { get; set; }

        public bool FindStartupType { get; set; }

        public bool PreventHostingStartup { get; set; }

        public IReadOnlyList<string> HostingStartupAssemblies { get; set; }

        public bool DetailedErrors { get; set; }

        public bool CaptureStartupErrors { get; set; }

        public string Environment { get; set; }

        public string StartupAssembly { get; set; }

        public string WebRoot { get; set; }

        public string ContentRootPath { get; set; }

        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

    }
}