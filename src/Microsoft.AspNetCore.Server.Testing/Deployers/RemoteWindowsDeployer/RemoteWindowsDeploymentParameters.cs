﻿// Copyright (c) .NET Foundation. All rights reserved.
// See License.txt in the project root for license information

namespace Microsoft.AspNetCore.Server.Testing
{
    public class RemoteWindowsDeploymentParameters : DeploymentParameters
    {
        public RemoteWindowsDeploymentParameters(
            string applicationPath,
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture runtimeArchitecture,
            string remoteServerFileSharePath,
            string remoteServerName,
            string remoteServerAccountName,
            string remoteServerAccountPassword,
            string remoteServerRelativeExecutablePath)
            : base(applicationPath, serverType, runtimeFlavor, runtimeArchitecture)
        {
            RemoteServerFileSharePath = remoteServerFileSharePath;
            ServerName = remoteServerName;
            ServerAccountName = remoteServerAccountName;
            ServerAccountPassword = remoteServerAccountPassword;
            RemoteServerRelativeExecutablePath = remoteServerRelativeExecutablePath;
        }

        public string ServerName { get; }

        public string ServerAccountName { get; }

        public string ServerAccountPassword { get; }

        /// <summary>
        /// The full path to the remote server's file share
        /// </summary>
        public string RemoteServerFileSharePath { get; }

        /// <summary>
        /// The relative path to the executable in the published output
        /// </summary>
        public string RemoteServerRelativeExecutablePath { get; }
    }
}
