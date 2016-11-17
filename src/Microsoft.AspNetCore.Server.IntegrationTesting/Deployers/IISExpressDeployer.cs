// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    /// <summary>
    /// Deployment helper for IISExpress.
    /// </summary>
    public class IISExpressDeployer : ApplicationDeployer
    {
        private Process _hostProcess;

        public IISExpressDeployer(DeploymentParameters deploymentParameters, ILogger logger)
            : base(deploymentParameters, logger)
        {
        }

        public bool IsWin8OrLater
        {
            get
            {
                var win8Version = new Version(6, 2);

                return (new Version(Extensions.Internal.RuntimeEnvironment.OperatingSystemVersion) >= win8Version);
            }
        }

        public bool Is64BitHost
        {
            get
            {
                return RuntimeInformation.OSArchitecture == Architecture.X64
                    || RuntimeInformation.OSArchitecture == Architecture.Arm64;
            }
        }

        public override DeploymentResult Deploy()
        {
            // Start timer
            StartTimer();

            // For now we always auto-publish. Otherwise we'll have to write our own local web.config for the HttpPlatformHandler
            DeploymentParameters.PublishApplicationBeforeDeployment = true;
            if (DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                DotnetPublish();
            }

            var contentRoot = DeploymentParameters.PublishApplicationBeforeDeployment ? DeploymentParameters.PublishedApplicationRootPath : DeploymentParameters.ApplicationPath;

            var uri = TestUriHelper.BuildTestUri(DeploymentParameters.ApplicationBaseUriHint);
            // Launch the host process.
            var hostExitToken = StartIISExpress(uri, contentRoot);

            return new DeploymentResult
            {
                ContentRoot = contentRoot,
                DeploymentParameters = DeploymentParameters,
                // Right now this works only for urls like http://localhost:5001/. Does not work for http://localhost:5001/subpath.
                ApplicationBaseUri = uri.ToString(),
                HostShutdownToken = hostExitToken
            };
        }

        private CancellationToken StartIISExpress(Uri uri, string contentRoot)
        {
            if (!string.IsNullOrWhiteSpace(DeploymentParameters.ServerConfigTemplateContent))
            {
                // Pass on the applicationhost.config to iis express. With this don't need to pass in the /path /port switches as they are in the applicationHost.config
                // We take a copy of the original specified applicationHost.Config to prevent modifying the one in the repo.

                if (DeploymentParameters.ServerConfigTemplateContent.Contains("[ANCMPath]"))
                {
                    string ancmPath;
                    // We need to pick the bitness based the OS / IIS Express, not the application.
                    // We'll eventually add support for choosing which IIS Express bitness to run: https://github.com/aspnet/Hosting/issues/880
                    var ancmFile = Is64BitHost ? "aspnetcore_x64.dll" : "aspnetcore_x86.dll";
                    if (!IsWin8OrLater)
                    {
                        // The nupkg build of ANCM does not support Win7. https://github.com/aspnet/AspNetCoreModule/issues/40.
                        ancmPath = @"%ProgramFiles%\IIS Express\aspnetcore.dll";
                    }
                    // Bin deployed by Microsoft.AspNetCore.AspNetCoreModule.nupkg
                    else if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr
                        && DeploymentParameters.ApplicationType == ApplicationType.Portable)
                    {
                        ancmPath = Path.Combine(contentRoot, @"runtimes\win7\native\", ancmFile);
                    }
                    else
                    {
                        ancmPath = Path.Combine(contentRoot, ancmFile);
                    }

                    if (!File.Exists(ancmPath))
                    {
                        throw new FileNotFoundException("AspNetCoreModule could not be found.", ancmPath);
                    }

                    DeploymentParameters.ServerConfigTemplateContent =
                        DeploymentParameters.ServerConfigTemplateContent.Replace("[ANCMPath]", ancmPath);
                }

                DeploymentParameters.ServerConfigTemplateContent =
                    DeploymentParameters.ServerConfigTemplateContent
                        .Replace("[ApplicationPhysicalPath]", contentRoot)
                        .Replace("[PORT]", uri.Port.ToString());

                DeploymentParameters.ServerConfigLocation = Path.GetTempFileName();

                File.WriteAllText(DeploymentParameters.ServerConfigLocation, DeploymentParameters.ServerConfigTemplateContent);
            }

            var parameters = string.IsNullOrWhiteSpace(DeploymentParameters.ServerConfigLocation) ?
                            string.Format("/port:{0} /path:\"{1}\" /trace:error", uri.Port, contentRoot) :
                            string.Format("/site:{0} /config:{1} /trace:error", DeploymentParameters.SiteName, DeploymentParameters.ServerConfigLocation);

            var iisExpressPath = GetIISExpressPath();

            Logger.LogInformation("Executing command : {iisExpress} {args}", iisExpressPath, parameters);

            var startInfo = new ProcessStartInfo
            {
                FileName = iisExpressPath,
                Arguments = parameters,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            AddEnvironmentVariablesToProcess(startInfo, DeploymentParameters.EnvironmentVariables);

            _hostProcess = new Process() { StartInfo = startInfo };
            _hostProcess.ErrorDataReceived += (sender, dataArgs) => { Logger.LogError(dataArgs.Data ?? string.Empty); };
            _hostProcess.OutputDataReceived += (sender, dataArgs) => { Logger.LogInformation(dataArgs.Data ?? string.Empty); };
            _hostProcess.EnableRaisingEvents = true;
            var hostExitTokenSource = new CancellationTokenSource();
            _hostProcess.Exited += (sender, e) =>
            {
                TriggerHostShutdown(hostExitTokenSource);
            };
            _hostProcess.Start();
            _hostProcess.BeginErrorReadLine();
            _hostProcess.BeginOutputReadLine();

            if (_hostProcess.HasExited)
            {
                Logger.LogError("Host process {processName} exited with code {exitCode} or failed to start.", startInfo.FileName, _hostProcess.ExitCode);
                throw new Exception("Failed to start host");
            }

            Logger.LogInformation("Started iisexpress. Process Id : {processId}", _hostProcess.Id);
            return hostExitTokenSource.Token;
        }

        private string GetIISExpressPath()
        {
            // Get path to program files
            var iisExpressPath = Path.Combine(Environment.GetEnvironmentVariable("SystemDrive") + "\\", "Program Files", "IIS Express", "iisexpress.exe");

            if (!File.Exists(iisExpressPath))
            {
                throw new Exception("Unable to find IISExpress on the machine: " + iisExpressPath);
            }

            return iisExpressPath;
        }

        public override void Dispose()
        {
            ShutDownIfAnyHostProcess(_hostProcess);

            if (!string.IsNullOrWhiteSpace(DeploymentParameters.ServerConfigLocation)
                && File.Exists(DeploymentParameters.ServerConfigLocation))
            {
                // Delete the temp applicationHostConfig that we created.
                try
                {
                    File.Delete(DeploymentParameters.ServerConfigLocation);
                }
                catch (Exception exception)
                {
                    // Ignore delete failures - just write a log.
                    Logger.LogWarning("Failed to delete '{config}'. Exception : {exception}", DeploymentParameters.ServerConfigLocation, exception.Message);
                }
            }

            if (DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                CleanPublishedOutput();
            }

            InvokeUserApplicationCleanup();

            StopTimer();
        }
    }
}