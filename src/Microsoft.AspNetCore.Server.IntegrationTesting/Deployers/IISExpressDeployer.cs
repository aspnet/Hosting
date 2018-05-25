﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    /// <summary>
    /// Deployment helper for IISExpress.
    /// </summary>
    public class IISExpressDeployer : ApplicationDeployer
    {
        private const string IISExpressRunningMessage = "IIS Express is running.";
        private const string FailedToInitializeBindingsMessage = "Failed to initialize site bindings";
        private const string UnableToStartIISExpressMessage = "Unable to start iisexpress.";
        private const int MaximumAttempts = 5;

        private static readonly Regex UrlDetectorRegex = new Regex(@"^\s*Successfully registered URL ""(?<url>[^""]+)"" for site.*$");

        private Process _hostProcess;
        private string _webConfig;

        public IISExpressDeployer(DeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
            : base(deploymentParameters, loggerFactory)
        {
        }

        public override async Task<DeploymentResult> DeployAsync()
        {
            using (Logger.BeginScope("Deployment"))
            {
                // Start timer
                StartTimer();

                if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr
                        && DeploymentParameters.RuntimeArchitecture == RuntimeArchitecture.x86)
                {
                    // Publish is required to rebuild for the right bitness
                    DeploymentParameters.PublishApplicationBeforeDeployment = true;
                }

                if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr
                        && DeploymentParameters.ApplicationType == ApplicationType.Standalone)
                {
                    // Publish is required to get the correct files in the output directory
                    DeploymentParameters.PublishApplicationBeforeDeployment = true;
                }

                var contentRoot = string.Empty;
                var dllRoot = string.Empty;
                if (DeploymentParameters.PublishApplicationBeforeDeployment)
                {
                    DotnetPublish();
                    contentRoot = DeploymentParameters.PublishedApplicationRootPath;
                    dllRoot = contentRoot;
                    _webConfig = Path.Combine(contentRoot, "web.config");
                }
                else
                {
                    // Core+Standalone always publishes. This must be Clr+Standalone or Core+Portable.
                    // Update processPath and arguments for our current scenario

                    contentRoot = DeploymentParameters.ApplicationPath;
                    _webConfig = Path.Combine(DeploymentParameters.ApplicationPath, "web.config");

                    var targetFramework = DeploymentParameters.TargetFramework;
                    dllRoot = Path.Combine(DeploymentParameters.ApplicationPath, "bin", DeploymentParameters.Configuration, targetFramework);

                    var executableExtension = DeploymentParameters.ApplicationType == ApplicationType.Portable ? ".dll" : ".exe";
                    var entryPoint = Path.Combine(dllRoot, DeploymentParameters.ApplicationName + executableExtension);

                    var executableName = string.Empty;
                    var executableArgs = string.Empty;

                    if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr && DeploymentParameters.ApplicationType == ApplicationType.Portable)
                    {
                        executableName = GetDotNetExeForArchitecture();
                        executableArgs = entryPoint;
                    }
                    else
                    {
                        executableName = entryPoint;
                    }

                    Logger.LogInformation("Executing: {exe} {args}", executableName, executableArgs);
                    DeploymentParameters.EnvironmentVariables["LAUNCHER_PATH"] = executableName;
                    DeploymentParameters.EnvironmentVariables["LAUNCHER_ARGS"] = executableArgs;

                    // CurrentDirectory will point to bin/{config}/{tfm}, but the config and static files aren't copied, point to the app base instead.
                    Logger.LogInformation("ContentRoot: {path}", DeploymentParameters.ApplicationPath);
                    DeploymentParameters.EnvironmentVariables["ASPNETCORE_CONTENTROOT"] = DeploymentParameters.ApplicationPath;
                }

                var testUri = TestUriHelper.BuildTestUri(ServerType.IISExpress, DeploymentParameters.ApplicationBaseUriHint);

                // Launch the host process.
                var (actualUri, hostExitToken) = await StartIISExpressAsync(testUri, contentRoot, dllRoot);

                Logger.LogInformation("Application ready at URL: {appUrl}", actualUri);

                // Right now this works only for urls like http://localhost:5001/. Does not work for http://localhost:5001/subpath.
                return new DeploymentResult(
                    LoggerFactory,
                    DeploymentParameters,
                    applicationBaseUri: actualUri.ToString(),
                    contentRoot: contentRoot,
                    hostShutdownToken: hostExitToken);
            }
        }

        private async Task<(Uri url, CancellationToken hostExitToken)> StartIISExpressAsync(Uri uri, string contentRoot, string dllRoot)
        {
            using (Logger.BeginScope("StartIISExpress"))
            {
                var port = uri.Port;
                if (port == 0)
                {
                    port = TestUriHelper.GetNextPort();
                }

                Logger.LogInformation("Attempting to start IIS Express on port: {port}", port);
                PrepareConfig(contentRoot, dllRoot, port);

                var parameters = string.IsNullOrWhiteSpace(DeploymentParameters.ServerConfigLocation) ?
                                string.Format("/port:{0} /path:\"{1}\" /trace:error", uri.Port, contentRoot) :
                                string.Format("/site:{0} /config:{1} /trace:error", DeploymentParameters.SiteName, DeploymentParameters.ServerConfigLocation);

                var iisExpressPath = GetIISExpressPath();

                for (var attempt = 0; attempt < MaximumAttempts; attempt++)
                {
                    Logger.LogInformation("Executing command : {iisExpress} {parameters}", iisExpressPath, parameters);

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

                    Uri url = null;
                    var started = new TaskCompletionSource<bool>();

                    var process = new Process() { StartInfo = startInfo };
                    process.OutputDataReceived += (sender, dataArgs) =>
                    {
                        if (string.Equals(dataArgs.Data, UnableToStartIISExpressMessage))
                        {
                            // We completely failed to start and we don't really know why
                            started.TrySetException(new InvalidOperationException("Failed to start IIS Express"));
                        }
                        else if (string.Equals(dataArgs.Data, FailedToInitializeBindingsMessage))
                        {
                            started.TrySetResult(false);
                        }
                        else if (string.Equals(dataArgs.Data, IISExpressRunningMessage))
                        {
                            started.TrySetResult(true);
                        }
                        else if (!string.IsNullOrEmpty(dataArgs.Data))
                        {
                            var m = UrlDetectorRegex.Match(dataArgs.Data);
                            if (m.Success)
                            {
                                url = new Uri(m.Groups["url"].Value);
                            }
                        }
                    };

                    process.EnableRaisingEvents = true;
                    var hostExitTokenSource = new CancellationTokenSource();
                    process.Exited += (sender, e) =>
                    {
                        Logger.LogInformation("iisexpress Process {pid} shut down", process.Id);

                        // If TrySetResult was called above, this will just silently fail to set the new state, which is what we want
                        started.TrySetException(new Exception($"Command exited unexpectedly with exit code: {process.ExitCode}"));

                        TriggerHostShutdown(hostExitTokenSource);
                    };
                    process.StartAndCaptureOutAndErrToLogger("iisexpress", Logger);
                    Logger.LogInformation("iisexpress Process {pid} started", process.Id);

                    if (process.HasExited)
                    {
                        Logger.LogError("Host process {processName} {pid} exited with code {exitCode} or failed to start.", startInfo.FileName, process.Id, process.ExitCode);
                        throw new Exception("Failed to start host");
                    }

                    // Wait for the app to start
                    // The timeout here is large, because we don't know how long the test could need
                    // We cover a lot of error cases above, but I want to make sure we eventually give up and don't hang the build
                    // just in case we missed one -anurse
                    if (!await started.Task.TimeoutAfter(TimeSpan.FromMinutes(10)))
                    {
                        Logger.LogInformation("iisexpress Process {pid} failed to bind to port {port}, trying again", process.Id, port);

                        // Wait for the process to exit and try again
                        process.WaitForExit(30 * 1000);
                        await Task.Delay(1000); // Wait a second to make sure the socket is completely cleaned up
                    }
                    else
                    {
                        _hostProcess = process;
                        Logger.LogInformation("Started iisexpress successfully. Process Id : {processId}, Port: {port}", _hostProcess.Id, port);
                        return (url: url, hostExitToken: hostExitTokenSource.Token);
                    }
                }

                var message = $"Failed to initialize IIS Express after {MaximumAttempts} attempts to select a port";
                Logger.LogError(message);
                throw new TimeoutException(message);
            }
        }

        private void PrepareConfig(string contentRoot, string dllRoot, int port)
        {
            // Config is required. If not present then fall back to one we cary with us.
            if (string.IsNullOrWhiteSpace(DeploymentParameters.ServerConfigTemplateContent))
            {
                using (var stream = GetType().Assembly.GetManifestResourceStream("Microsoft.AspNetCore.Server.IntegrationTesting.Http.config"))
                using (var reader = new StreamReader(stream))
                {
                    DeploymentParameters.ServerConfigTemplateContent = reader.ReadToEnd();
                }
            }

            var serverConfig = DeploymentParameters.ServerConfigTemplateContent;

            // Pass on the applicationhost.config to iis express. With this don't need to pass in the /path /port switches as they are in the applicationHost.config
            // We take a copy of the original specified applicationHost.Config to prevent modifying the one in the repo.
            serverConfig = ModifyANCMPathInConfig(replaceFlag: "[ANCMPath]", dllName: "aspnetcore.dll", serverConfig, dllRoot);
            serverConfig = ModifyANCMPathInConfig(replaceFlag: "[ANCMV2Path]", dllName: "aspnetcorev2.dll", serverConfig, dllRoot);

            serverConfig = ReplacePlaceholder(serverConfig, "[PORT]", port.ToString(CultureInfo.InvariantCulture));
            serverConfig = ReplacePlaceholder(serverConfig, "[ApplicationPhysicalPath]", contentRoot);

            if (DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                // For published apps, prefer the content in the web.config, but update it.
                ModifyAspNetCoreSectionInWebConfig(key: "hostingModel",
                    value: DeploymentParameters.HostingModel == HostingModel.InProcess ? "inprocess" : "");
                ModifyHandlerSectionInWebConfig(key: "modules", value: DeploymentParameters.AncmVersion.ToString());
                ModifyDotNetExePathInWebConfig();
                serverConfig = RemoveRedundantElements(serverConfig);
            }
            else
            {
                // The elements normally in the web.config are in the applicationhost.config for unpublished apps.
                serverConfig = ReplacePlaceholder(serverConfig, "[HostingModel]", DeploymentParameters.HostingModel.ToString());
                serverConfig = ReplacePlaceholder(serverConfig, "[AspNetCoreModule]", DeploymentParameters.AncmVersion.ToString());
            }

            DeploymentParameters.ServerConfigLocation = Path.GetTempFileName();
            Logger.LogDebug("Saving Config to {configPath}", DeploymentParameters.ServerConfigLocation);

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace($"Config File Content:{Environment.NewLine}===START CONFIG==={Environment.NewLine}{{configContent}}{Environment.NewLine}===END CONFIG===", serverConfig);
            }

            File.WriteAllText(DeploymentParameters.ServerConfigLocation, serverConfig);
        }

        private string ReplacePlaceholder(string content, string field, string value)
        {
            if (content.Contains(field))
            {
                content = content.Replace(field, value);
                Logger.LogDebug("Writing {field} '{value}' to config", field, value);
            }
            return content;
        }

        private string ModifyANCMPathInConfig(string replaceFlag, string dllName, string serverConfig, string dllRoot)
        {
            if (serverConfig.Contains(replaceFlag))
            {
                var arch = DeploymentParameters.RuntimeArchitecture == RuntimeArchitecture.x64 ? $@"x64\{dllName}" : $@"x86\{dllName}";
                var ancmFile = Path.Combine(dllRoot, arch);
                if (!File.Exists(Environment.ExpandEnvironmentVariables(ancmFile)))
                {
                    ancmFile = Path.Combine(dllRoot, dllName);
                    if (!File.Exists(Environment.ExpandEnvironmentVariables(ancmFile)))
                    {
                        throw new FileNotFoundException("AspNetCoreModule could not be found.", ancmFile);
                    }
                }

                Logger.LogDebug($"Writing '{replaceFlag}' '{ancmFile}' to config");
                return serverConfig.Replace(replaceFlag, ancmFile);
            }
            return serverConfig;
        }

        private string GetIISExpressPath()
        {
            var programFiles = "Program Files";
            if (DotNetCommands.IsRunningX86OnX64(DeploymentParameters.RuntimeArchitecture))
            {
                programFiles = "Program Files (x86)";
            }

            // Get path to program files
            var iisExpressPath = Path.Combine(Environment.GetEnvironmentVariable("SystemDrive") + "\\", programFiles, "IIS Express", "iisexpress.exe");

            if (!File.Exists(iisExpressPath))
            {
                throw new Exception("Unable to find IISExpress on the machine: " + iisExpressPath);
            }

            return iisExpressPath;
        }

        public override void Dispose()
        {
            using (Logger.BeginScope("Dispose"))
            {
                ShutDownIfAnyHostProcess(_hostProcess);

                if (!string.IsNullOrWhiteSpace(DeploymentParameters.ServerConfigLocation)
                    && File.Exists(DeploymentParameters.ServerConfigLocation))
                {
                    // Delete the temp applicationHostConfig that we created.
                    Logger.LogDebug("Deleting applicationHost.config file from {configLocation}", DeploymentParameters.ServerConfigLocation);
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

            // If by this point, the host process is still running (somehow), throw an error.
            // A test failure is better than a silent hang and unknown failure later on
            if (_hostProcess != null && !_hostProcess.HasExited)
            {
                throw new Exception($"iisexpress Process {_hostProcess.Id} failed to shutdown");
            }
        }

        private void ModifyDotNetExePathInWebConfig()
        {
            // We assume the x64 dotnet.exe is on the path so we need to provide an absolute path for x86 scenarios.
            // Only do it for scenarios that rely on dotnet.exe (Core, portable, etc.).
            if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr
                && DeploymentParameters.ApplicationType == ApplicationType.Portable
                && DotNetCommands.IsRunningX86OnX64(DeploymentParameters.RuntimeArchitecture))
            {
                var executableName = DotNetCommands.GetDotNetExecutable(DeploymentParameters.RuntimeArchitecture);
                if (!File.Exists(executableName))
                {
                    throw new Exception($"Unable to find '{executableName}'.'");
                }
                ModifyAspNetCoreSectionInWebConfig("processPath", executableName);
            }
        }

        // Transforms the web.config file to set attributes like hostingModel="inprocess" element
        private void ModifyAspNetCoreSectionInWebConfig(string key, string value)
        {
            var config = XDocument.Load(_webConfig);
            var element = config.Descendants("aspNetCore").FirstOrDefault();
            element.SetAttributeValue(key, value);
            config.Save(_webConfig);
        }

        private void ModifyHandlerSectionInWebConfig(string key, string value)
        {
            var config = XDocument.Load(_webConfig);
            var element = config.Descendants("handlers").FirstOrDefault().Descendants("add").FirstOrDefault();
            element.SetAttributeValue(key, value);
            config.Save(_webConfig);
        }

        // These elements are duplicated in the web.config if you publish. Remove them from the host.config.
        private string RemoveRedundantElements(string serverConfig)
        {
            var hostConfig = XDocument.Parse(serverConfig);

            var coreElement = hostConfig.Descendants("aspNetCore").FirstOrDefault();
            coreElement?.Remove();

            var handlersElement = hostConfig.Descendants("handlers").First();
            var handlerElement = handlersElement.Descendants("add")
                .Where(x => x.Attribute("name").Value == "aspNetCore").FirstOrDefault();
            handlerElement?.Remove();

            return hostConfig.ToString();
        }
    }
}
