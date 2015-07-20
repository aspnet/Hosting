// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingEnvironmentExtensions
    {
        private const string DevelopmentEnvironmentName = "Development";
        private const string ProductionEnvironmentName = "Production";

        /// <summary>
        /// Checks if the current hosting environment name is development.
        /// </summary>
        /// <param name="hostingEnvironment">An instance of <see cref="IHostingEnvironment"/> service.</param>
        /// <returns>True if the environment name is Development, otherwise false.</returns>
        public static bool IsDevelopment([NotNull]this IHostingEnvironment hostingEnvironment)
        {
            return hostingEnvironment.IsEnvironment(DevelopmentEnvironmentName);
        }

        /// <summary>
        /// Checks if the current hosting environment name is Production.
        /// </summary>
        /// <param name="hostingEnvironment">An instance of <see cref="IHostingEnvironment"/> service.</param>
        /// <returns>True if the environment name is Production, otherwise false.</returns>
        public static bool IsProduction([NotNull]this IHostingEnvironment hostingEnvironment)
        {
            return hostingEnvironment.IsEnvironment(ProductionEnvironmentName);
        }

        /// <summary>
        /// Compares the current hosting environment name against the specified value.
        /// </summary>
        /// <param name="hostingEnvironment">An instance of <see cref="IHostingEnvironment"/> service.</param>
        /// <param name="environmentName">Environment name to validate against.</param>
        /// <returns>True if the specified name is same as the current environment.</returns>
        public static bool IsEnvironment(
            [NotNull]this IHostingEnvironment hostingEnvironment,
            string environmentName)
        {
            return string.Equals(
                hostingEnvironment.EnvironmentName,
                environmentName,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gives the physical path corresponding to the given virtual path.
        /// </summary>
        /// <param name="hostingEnvironment">An instance of <see cref="IHostingEnvironment"/> service.</param>
        /// <param name="virtualPath">Path relative to the root.</param>
        /// <returns>Physical path corresponding to virtual path.</returns>
        public static string MapPath(
            [NotNull]this IHostingEnvironment hostingEnvironment,
            string virtualPath)
        {
            if (string.IsNullOrEmpty(virtualPath) || string.CompareOrdinal(virtualPath, "~") == 0)
            {
                return hostingEnvironment.WebRootPath;
            }

            // "~/index.html" -> "index.html" (makes it relative to the web root)
            // "~\index.html" -> "index.html" (makes it relative to the web root)
            if (virtualPath.StartsWith("~/", StringComparison.OrdinalIgnoreCase) ||
                virtualPath.StartsWith("~\\", StringComparison.OrdinalIgnoreCase))
            {
                virtualPath = virtualPath.Substring(2);
            }

            // "/index.html" -> "index.html" (makes it relative to the web root)
            var pathStartIdx = 0;
            while (pathStartIdx < virtualPath.Length && (virtualPath[pathStartIdx] == '\\' || virtualPath[pathStartIdx] == '/'))
            {
                pathStartIdx++;
            }

            virtualPath = virtualPath.Substring(pathStartIdx);

            var normalizedPath = Path.GetFullPath(Path.Combine(hostingEnvironment.WebRootPath, virtualPath));

            if (normalizedPath.Length < hostingEnvironment.WebRootPath.Length)
            {
                throw new InvalidOperationException("The mapped path is above the application directory.");
            }

            return normalizedPath;
        }
    }
}