// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ServiceProcess;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting.WindowsServices
{
    /// <summary>
    ///     Extensions to <see cref="IWebHost"/> for hosting inside a Windows service.
    /// </summary>
    public static class WebHostWindowsServiceExtensions
    {
        /// <summary>
        ///     Runs the specified web application inside a Windows service and blocks until the service is stopped.
        /// </summary>
        /// <param name="host">An instance of the <see cref="IWebHost"/> to host in the Windows service.</param>
        /// <example>
        ///     This example shows how to use <see cref="RunAsService(IWebHost)"/>.
        ///     <code>
        ///         public class Program
        ///         {
        ///             public static void Main(string[] args)
        ///             {
        ///                 var config = WebHostConfiguration.GetDefault(args);
        ///                 
        ///                 var host = new WebHostBuilder()
        ///                     .UseConfiguration(config)
        ///                     .Build();
        ///          
        ///                 // This call will block until the service is stopped.
        ///                 host.RunAsService();
        ///             }
        ///         }
        ///     </code>
        /// </example>
        public static void RunAsService(this IWebHost host)
        {
            var webHostService = new WebHostService(host);
            ServiceBase.Run(webHostService);
        }

        /// <summary>
        ///     Runs the specified .net core application inside a Windows service and blocks until the service is stopped.
        /// </summary>
        /// <param name="host">An instance of the <see cref="IHost"/> to host in the Windows service.</param>
        /// <example>
        ///     This example shows how to use <see cref="RunAsService(IHost)"/>.
        ///     <code>
        ///         public class Program
        ///         {
        ///             public static void Main(string[] args)
        ///             {
        ///                 var host = new HostBuilder()
        ///                     .ConfigureHostConfiguration(configHost =>
        ///                     {
        ///                         configHost.SetBasePath(Directory.GetCurrentDirectory());
        ///                         configHost.AddEnvironmentVariables(prefix: "ASPNETCORE_");
        ///                         configHost.AddCommandLine(args);
        ///                     })
        ///                     .ConfigureAppConfiguration((hostContext, configApp) =>
        ///                     {
        ///                         configApp.AddJsonFile("appsettings.json", optional: true);
        ///                         configApp.AddEnvironmentVariables(prefix: "ASPNETCORE_");
        ///                         configApp.AddCommandLine(args);
        ///                     })
        ///                     .ConfigureServices((hostContext, services) =>
        ///                     {
        ///                         services.AddLogging();               ///                       
        ///                     })
        ///                     .ConfigureLogging((hostContext, configLogging) =>
        ///                     {
        ///                         configLogging.AddConsole()        
        ///                     })
        ///                     .UseConsoleLifetime()
        ///                     .Build();        
        ///                  
        ///                 // This call will block until the service is stopped.
        ///                 host.RunAsService();
        ///             }
        ///         }
        ///     </code>
        /// </example> 
        public static void RunAsService(this IHost host)
        {
            var genericHostService = new GenericHostService(host);
            ServiceBase.Run(genericHostService);
        }
    }
}
