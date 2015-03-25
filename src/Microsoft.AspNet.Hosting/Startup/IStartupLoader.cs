// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Hosting.Startup
{
    public interface IStartupLoader
    {
        StartupMethods Load(
            IServiceProvider services,
            string startupClass,
            string environmentName,
            IList<string> diagnosticMessages);

        StartupMethods Load(
            IServiceProvider services,
            Type startupClass,
            string environmentName,
            IList<string> diagnosticMessages);
    }
}
