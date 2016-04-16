// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Microsoft.AspNetCore.Server.Testing
{
    public class NanoDeployer : ApplicationDeployer
    {
        /// <summary>
        /// Example: If the share path is '\\foo\bar', then this returns the full path to the
        /// deployed folder. Example: '\\foo\bar\048f6c99-de3e-488a-8020-f9eb277818d9'
        /// </summary>
        private string _deployedFolderPathInFileShare;
        private readonly NanoDeploymentParameters _deploymentParameters;
        private string _remotePSSessionHelperScript;

        public NanoDeployer(DeploymentParameters deploymentParameters, ILogger logger)
            : base(deploymentParameters, logger)
        {
            _deploymentParameters = DeploymentParameters as NanoDeploymentParameters;
            if (_deploymentParameters == null)
            {
                throw new InvalidOperationException($"Expected the deployment parameters of type {nameof(NanoDeploymentParameters)}.");
            }

            if (_deploymentParameters.ServerType != ServerType.IIS
                && _deploymentParameters.ServerType != ServerType.Kestrel
                && _deploymentParameters.ServerType != ServerType.WebListener)
            {
                throw new InvalidOperationException($"Server type {_deploymentParameters.ServerType} is not supported for Nano." +
                    " Supported server types are Kestrel, IIS and WebListener");
            }

            if (string.IsNullOrWhiteSpace(_deploymentParameters.ServerName))
            {
                throw new ArgumentException($"Invalid value for {nameof(NanoDeploymentParameters.ServerName)}");
            }

            if (string.IsNullOrWhiteSpace(_deploymentParameters.ServerAccountName))
            {
                throw new ArgumentException($"Invalid value for {nameof(NanoDeploymentParameters.ServerAccountName)}");
            }

            if (string.IsNullOrWhiteSpace(_deploymentParameters.ServerAccountPassword))
            {
                throw new ArgumentException($"Invalid value for {nameof(NanoDeploymentParameters.ServerAccountPassword)}");
            }

            if (string.IsNullOrWhiteSpace(_deploymentParameters.NanoServerFileShare))
            {
                throw new ArgumentException($"Invalid value for {nameof(NanoDeploymentParameters.NanoServerFileShare)}");
            }

            if (string.IsNullOrWhiteSpace(_deploymentParameters.NanoServerRelativeExecutablePath))
            {
                throw new ArgumentException($"Invalid value for {nameof(NanoDeploymentParameters.NanoServerRelativeExecutablePath)}");
            }
        }

        public override DeploymentResult Deploy()
        {
            // Publish the app to a local temp folder on the machine where the test is running
            DotnetPublish();

            var folderId = Guid.NewGuid().ToString();
            _deployedFolderPathInFileShare = Path.Combine(_deploymentParameters.NanoServerFileShare, folderId);

            DirectoryCopy(
                _deploymentParameters.PublishedApplicationRootPath,
                _deployedFolderPathInFileShare,
                copySubDirs: true);
            Logger.LogInformation($"Copied the locally published folder to the file share path '{_deployedFolderPathInFileShare}'");

            // Copy the scripts from this assembly's embedded resources to the temp path on the machine where these
            // tests are being run
            var embeddedFileProvider = new EmbeddedFileProvider(
                GetType().GetTypeInfo().Assembly,
                "Microsoft.AspNetCore.Server.Testing.Deployers.NanoDeployer");

            var filesOnDisk = CopyEmbeddedScriptFilesToDisk(
                embeddedFileProvider,
                new[] { "RemotePSSessionHelper.ps1", "StartServer.ps1", "StopServer.ps1" });
            _remotePSSessionHelperScript = filesOnDisk[0];

            RunScript("StartServer");

            string uri;
            if (_deploymentParameters.ServerType == ServerType.IIS)
            {
                uri = $"http://{_deploymentParameters.ServerName}:80/";
            }
            else
            {
                uri = $"http://{_deploymentParameters.ServerName}:5000/";
            }

            return new DeploymentResult
            {
                ApplicationBaseUri = uri,
                DeploymentParameters = DeploymentParameters
            };
        }

        public override void Dispose()
        {
            try
            {
                Logger.LogInformation("Stopping the server...");
                RunScript("StopServer");
            }
            catch { }

            try
            {
                Logger.LogInformation($"Deleting the deployed folder '{_deployedFolderPathInFileShare}'");
                Directory.Delete(_deployedFolderPathInFileShare, recursive: true);
            }
            catch { }

            try
            {
                Logger.LogInformation($"Deleting the locally published folder '{DeploymentParameters.PublishedApplicationRootPath}'");
                Directory.Delete(DeploymentParameters.PublishedApplicationRootPath, recursive: true);
            }
            catch { }
        }

        private void RunScript(string serverAction)
        {
            var parameterBuilder = new StringBuilder();
            parameterBuilder.Append($"\"{_remotePSSessionHelperScript}\"");
            parameterBuilder.Append($" -serverName {_deploymentParameters.ServerName}");
            parameterBuilder.Append($" -accountName {_deploymentParameters.ServerAccountName}");
            parameterBuilder.Append($" -accountPassword {_deploymentParameters.ServerAccountPassword}");
            parameterBuilder.Append($" -executablePath {Path.Combine(_deployedFolderPathInFileShare, _deploymentParameters.NanoServerRelativeExecutablePath)}");
            parameterBuilder.Append($" -serverType {_deploymentParameters.ServerType}");
            parameterBuilder.Append($" -serverAction {serverAction}");

            // todo: launch a powershell process to make the website point to the created folder
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = parameterBuilder.ToString(),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };

            using (var deployToNanoProcess = new Process() { StartInfo = startInfo })
            {
                deployToNanoProcess.EnableRaisingEvents = true;
                deployToNanoProcess.ErrorDataReceived += (sender, dataArgs) =>
                {
                    if (!string.IsNullOrEmpty(dataArgs.Data))
                    {
                        Logger.LogWarning("Nano server: " + dataArgs.Data);
                    }
                };

                deployToNanoProcess.OutputDataReceived += (sender, dataArgs) =>
                {
                    if (!string.IsNullOrEmpty(dataArgs.Data))
                    {
                        Logger.LogInformation("Nano server: " + dataArgs.Data);
                    }
                };

                deployToNanoProcess.Start();
                deployToNanoProcess.BeginErrorReadLine();
                deployToNanoProcess.BeginOutputReadLine();
                deployToNanoProcess.WaitForExit((int)TimeSpan.FromMinutes(1).TotalMilliseconds);

                if (deployToNanoProcess.HasExited && deployToNanoProcess.ExitCode != 0)
                {
                    throw new Exception("Failed to deploy to Nano server.");
                }
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            var dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            var dirs = dir.GetDirectories();
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            var files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                var temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (var subdir in dirs)
                {
                    var temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private static string[] CopyEmbeddedScriptFilesToDisk(
            IFileProvider embeddedFileProvider,
            string[] embeddedFileNames)
        {
            var filesOnDisk = new string[embeddedFileNames.Length];
            for (var i = 0; i < embeddedFileNames.Length; i++)
            {
                var embeddedFileName = embeddedFileNames[i];
                var physicalFilePath = Path.Combine(Path.GetTempPath(), embeddedFileName);
                var sourceStream = embeddedFileProvider
                    .GetFileInfo(embeddedFileName)
                    .CreateReadStream();

                using (sourceStream)
                {
                    var destinationStream = File.Create(physicalFilePath);
                    using (destinationStream)
                    {
                        sourceStream.CopyTo(destinationStream);
                    }
                }

                filesOnDisk[i] = physicalFilePath;
            }

            return filesOnDisk;
        }
    }
}
