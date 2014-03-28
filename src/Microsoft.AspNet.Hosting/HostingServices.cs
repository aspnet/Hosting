using System.Collections.Generic;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingServices
    {
        public static IEnumerable<IServiceDescriptor> GetDefaultServices()
        {
            return GetDefaultServices(new Configuration());
        }

        public static IEnumerable<IServiceDescriptor> GetDefaultServices(IConfiguration configuration)
        {
            var describe = new ServiceDescriber(configuration);

            yield return describe.Transient<IHostingEngine, HostingEngine>();
            yield return describe.Transient<IServerFactoryProvider, ServerFactoryProvider>();

            yield return describe.Transient<IStartupManager, StartupManager>();
            yield return describe.Transient<IStartupLoaderProvider, StartupLoaderProvider>();

            yield return describe.Transient<IBuilderFactory, BuilderFactory>();
            yield return describe.Transient<IHttpContextFactory, HttpContextFactory>();

            yield return describe.Transient<ITypeActivator, TypeActivator>();
        }
    }
}