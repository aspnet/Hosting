// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class HostingApplicationData : IHostingApplicationData
    {
        public string ApplicationDataPath { get; set; }

        public IFileProvider ApplicationDataFileProvider { get; set; }

        internal void Initialize(WebHostOptions options)
        {
            CreateHostingApplicationData(options.ApplicationDataPath);
        }

        private void CreateHostingApplicationData(string basePath)
        {
            var appDataPath = ResolveApplicationDataPath(basePath);
            if (appDataPath != null)
            {
                ApplicationDataPath = appDataPath;
                ApplicationDataFileProvider = new PhysicalFileProvider(appDataPath);
            }
        }

        private string ResolveApplicationDataPath(string applicationDataPath)
        {
            var directoryInfo = GetOrCreateDirectory(applicationDataPath);
            if (directoryInfo != null)
            {
                return directoryInfo.FullName;
            }

            // Environment.GetFolderPath returns null if the user profile isn't loaded.
            var localAppDataFromSystemPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var localAppDataFromEnvPath = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            var userProfilePath = Environment.GetEnvironmentVariable("USERPROFILE");
            var homePath = Environment.GetEnvironmentVariable("HOME");

            // To preserve backwards-compatibility with 1.x, Environment.SpecialFolder.LocalApplicationData
            // cannot take precedence over $LOCALAPPDATA and $HOME/.aspnet on non-Windows platforms.
            // We do this in here too to keep consistency with the locations where data protection stores keys.
            directoryInfo = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                GetOrCreateDirectory(GetFolderPath(localAppDataFromSystemPath, "ASP.NET")) :
                null;

            directoryInfo = directoryInfo ??
                GetOrCreateDirectory(GetFolderPath(localAppDataFromEnvPath, "ASP.NET")) ??
                GetOrCreateDirectory(GetFolderPath(userProfilePath, "AppData", "Local", "ASP.NET")) ??
                GetOrCreateDirectory(GetFolderPath(homePath, ".aspnet")) ??
                GetOrCreateDirectory(GetFolderPath(localAppDataFromSystemPath, "ASP.NET")) ??
                GetOrCreateDirectory(GetFolderPath(AppContext.BaseDirectory, "APP_DATA"));

            return directoryInfo?.FullName;

            DirectoryInfo GetOrCreateDirectory(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }
                try
                {
                    var directory = new DirectoryInfo(path);
                    directory.Create(); // throws if we don't have access, e.g., user profile not loaded
                    return directory;
                }
                catch
                {
                    return null;
                }
            }

            string GetFolderPath(string basePath, params string[] path)
                => string.IsNullOrEmpty(basePath) ? null : Path.Combine(new[] { basePath }.Concat(path).ToArray());
        }
    }
}
