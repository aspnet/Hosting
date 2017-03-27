// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
#if NETSTANDARD1_3
using System.Runtime.InteropServices;
#endif
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public class PortHelper
    {
        public static void LogPortStatus(ILogger logger, int port)
        {
            logger.LogInformation("Checking for processes currently using port {0}", port);

            var psi = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            if (UseNetstat())
            {
                psi.FileName = "cmd";
                psi.Arguments = $"/C netstat -nq | find \"{port}\"";
            }
            else
            {
                psi.FileName = "lsof";
                psi.Arguments = $"-i :{port}";
            }

            var process = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            var linesLogged = false;

            process.OutputDataReceived += (sender, data) =>
            {
                linesLogged = linesLogged || !string.IsNullOrWhiteSpace(data.Data);
                logger.LogInformation("portstatus: {0}", data.Data ?? string.Empty);
            };
            process.ErrorDataReceived += (sender, data) => logger.LogWarning("portstatus: {0}", data.Data ?? string.Empty);

            try
            {
                process.Start();
                process.WaitForExit();

                if (!linesLogged)
                {
                    logger.LogInformation("portstatus: it appears the port {0} is not in use.", port);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to check port status. Executed: {0} {1}\nError: {2}", psi.FileName, psi.Arguments, ex.ToString());
            }
            return;
        }

        private static bool UseNetstat()
        {
#if NET46
            return true;
#elif NETSTANDARD1_3
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
#error Target frameworks need to be updated
#endif
        }
    }
}
