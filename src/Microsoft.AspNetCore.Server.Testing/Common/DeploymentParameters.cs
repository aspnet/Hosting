// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore.Server.Testing
{
    /// <summary>
    /// Parameters to control application deployment.
    /// </summary>
    public class DeploymentParameters
    {
        /// <summary>
        /// Creates an instance of <see cref="DeploymentParameters"/>.
        /// </summary>
        /// <param name="applicationPath">Source code location of the target location to be deployed.</param>
        /// <param name="serverType">Where to be deployed on.</param>
        /// <param name="runtimeFlavor">Flavor of the clr to run against.</param>
        /// <param name="runtimeArchitecture">Architecture of the DNX to be used.</param>
        public DeploymentParameters(
            string applicationPath,
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture runtimeArchitecture)
        {
            if (string.IsNullOrEmpty(applicationPath))
            {
                throw new ArgumentException("Value cannot be null.", nameof(applicationPath));
            }

            if (!Directory.Exists(applicationPath))
            {
                throw new DirectoryNotFoundException(string.Format("Application path {0} does not exist.", applicationPath));
            }

            if (runtimeArchitecture == RuntimeArchitecture.x86)
            {
                throw new NotSupportedException("32 bit compilation is not yet supported. Don't remove the tests, just disable them for now.");
            }

            ApplicationPath = applicationPath;
            ServerType = serverType;
            RuntimeFlavor = runtimeFlavor;
            EnvironmentVariables.Add(new KeyValuePair<string, string>("ASPNETCORE_DETAILEDERRORS", "true"));
        }

        public ServerType ServerType { get; }

        public RuntimeFlavor RuntimeFlavor { get; }

        public RuntimeArchitecture RuntimeArchitecture { get; } = RuntimeArchitecture.x64;

        /// <summary>
        /// Suggested base url for the deployed application. The final deployed url could be
        /// different than this. Use <see cref="DeploymentResult.ApplicationBaseUri"/> for the 
        /// deployed url.
        /// </summary>
        public string ApplicationBaseUriHint { get; set; }

        public string EnvironmentName { get; set; }

        public string ServerConfigTemplateContent { get; set; }

        public string ServerConfigLocation { get; set; }

        public string SiteName { get; set; }

        public string ApplicationPath { get; set; }

        public string PublishTargetFramework { get; set; }

        public string PublishedApplicationRootPath { get; set; }

        /// <summary>
        /// Environment variables to be set before starting the host.
        /// Not applicable for IIS Scenarios.
        /// </summary>
        public List<KeyValuePair<string, string>> EnvironmentVariables { get; private set; } = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// For any application level cleanup to be invoked after performing host cleanup.
        /// </summary>
        public Action<DeploymentParameters> UserAdditionalCleanup { get; set; }

        public override string ToString()
        {
            return string.Format(
                    "[Variation] :: ServerType={0}, Runtime={1}, Arch={2}, BaseUrlHint={3}",
                    ServerType, 
                    RuntimeFlavor, 
                    RuntimeArchitecture, 
                    ApplicationBaseUriHint);
        }
    }
}