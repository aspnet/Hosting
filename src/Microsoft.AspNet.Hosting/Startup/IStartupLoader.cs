// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Hosting.Startup
{
    public interface IStartupLoader
    {
        // REVIEW: Could remove environmentName and it service could get it from IHostingEnvironment
        StartupMethods Load(
            string startupAssemblyName,
            string environmentName,
            IList<string> diagnosticMessages);
    }
}
