// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Hosting
{
    public interface IHostingApplicationData
    {
        /// <summary>
        /// Gets or sets the absolute path to the directory that applications use to
        /// store data outside of the content root of the application.
        /// </summary>
        string ApplicationDataPath { get; set; }

        /// <summary>
        /// Gets or sets an <see cref="IFileProvider"/> pointing at <see cref="ApplicationDataPath"/>.
        /// </summary>
        IFileProvider ApplicationDataFileProvider { get; set; }
    }
}
