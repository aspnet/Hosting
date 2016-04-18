// Copyright (c) .NET Foundation. All rights reserved.
// See License.txt in the project root for license information

namespace Microsoft.AspNetCore.Server.Testing
{
    public class RemoteDeploymentParameters : DeploymentParameters
    {
        public RemoteDeploymentParameters(
            string applicationPath,
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture runtimeArchitecture,
            string remoteServerFileShare,
            string remoteServerName,
            string remoteServerAccountName,
            string remoteServerAccountPassword,
            string remoteServerRelativeExecutablePath)
            : base(applicationPath, serverType, runtimeFlavor, runtimeArchitecture)
        {
            RemoteServerFileShare = remoteServerFileShare;
            ServerName = remoteServerName;
            ServerAccountName = remoteServerAccountName;
            ServerAccountPassword = remoteServerAccountPassword;
            RemoteServerRelativeExecutablePath = remoteServerRelativeExecutablePath;
        }

        public string ServerName { get; }

        public string ServerAccountName { get; }

        public string ServerAccountPassword { get; }

        public string RemoteServerFileShare { get; }

        public string RemoteServerRelativeExecutablePath { get; }
    }
}
