// Copyright (c) .NET Foundation. All rights reserved.
// See License.txt in the project root for license information

namespace Microsoft.AspNetCore.Server.Testing
{
    public class NanoDeploymentParameters : DeploymentParameters
    {
        public NanoDeploymentParameters(
            string applicationPath,
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture runtimeArchitecture,
            string nanoServerFileShare,
            string nanoServerName,
            string nanoServerAccountName,
            string nanoServerAccountPassword,
            string nanoServerRelativeExecutablePath)
            : base(applicationPath, serverType, runtimeFlavor, runtimeArchitecture)
        {
            NanoServerFileShare = nanoServerFileShare;
            ServerName = nanoServerName;
            ServerAccountName = nanoServerAccountName;
            ServerAccountPassword = nanoServerAccountPassword;
            NanoServerRelativeExecutablePath = nanoServerRelativeExecutablePath;
        }

        public string ServerName { get; }

        public string ServerAccountName { get; }

        public string ServerAccountPassword { get; }

        public string NanoServerFileShare { get; }

        public string NanoServerRelativeExecutablePath { get; }
    }
}
