﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal interface ISupportsStartup
    {
        IWebHostBuilder Configure(Action<IApplicationBuilder> configure);
        IWebHostBuilder UseStartup(Type startupType);
    }
}
