// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.FunctionalTests
{
    public class DeploymentParametersTest
    {
        [Fact]
        public void ItPicksDefaultConfigBasedOnCompilationCondition()
        {
#if DEBUG
            const string config = "Debug";
#else
            const string config = "Release";
#endif

            Assert.Equal(config, new DeploymentParameters(AppContext.BaseDirectory, ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64).Configuration);
        }
    }
}
