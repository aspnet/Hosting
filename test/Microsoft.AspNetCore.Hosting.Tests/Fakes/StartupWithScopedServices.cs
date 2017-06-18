using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using static Microsoft.AspNetCore.Hosting.Tests.StartupManagerTests;

namespace Microsoft.AspNetCore.Hosting.Tests.Fakes
{
    public class StartupWithScopedServices
    {
        public void Configure(IApplicationBuilder builder, DisposableService disposable)
        {

        }
    }
}
