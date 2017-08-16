using System;
using Microsoft.AspNetCore.Hosting.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Tests.Fakes
{
    class StartupCaseInsensitive
    {
        public StartupCaseInsensitive()
        {
        }

        public static void ConfigureCaseInsensitiveServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "CaseInsensitive";
            });

        }

        public static void ConfigureCaseInsensitive(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "CaseInsensitive";
            });
        }

        public static IServiceProvider ConfigureCaseInsensitiveContainer(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "CaseInsensitive";
            });

            return services.BuildServiceProvider();
        }
    }
}
