// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Hosting.Startup
{
    public interface IStartupLoader
    {
        Type FindStartupType(string startupName, IList<string> diagnosticMessages);

        StartupMethods LoadMethods(Type startupType, IList<string> diagnosticMessages);
    }
}
