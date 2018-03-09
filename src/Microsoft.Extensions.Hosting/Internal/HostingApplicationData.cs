// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Hosting.Internal
{
    public class HostingApplicationData : IHostingApplicationData
    {
        public string ApplicationDataPath { get; set; }

        public IFileProvider ApplicationDataFileProvider { get; set; }
    }
}
