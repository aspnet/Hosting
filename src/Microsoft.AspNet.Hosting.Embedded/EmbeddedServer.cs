using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Hosting.Embedded
{
    public class EmbeddedServer : IServerFactory, IDisposable
    {
        private static readonly string ServerName = "Microsoft.AspNet.Host.Embedded";
        private Func<object, Task> _appDelegate = null;

        public static EmbeddedServer Create<TStartup>()
        {
            var fakeKlrServiceProvider = new ServiceCollection()
                                 .AddSingleton<IApplicationEnvironment, EmbeddedApplicationEnvironment>()
                                 .BuildServiceProvider();

            return Create<TStartup>(fakeKlrServiceProvider);
        }

        public static EmbeddedServer Create<TStartup>(IServiceProvider provider)
        {
            var startupLoader = new StartupLoader(provider, new NullStartupLoader());
            var name = typeof(TStartup).AssemblyQualifiedName;
            var diagnosticMessages = new List<string>();
            return Create(provider, startupLoader.LoadStartup(name, diagnosticMessages));
        }

        public static EmbeddedServer Create(Action<IBuilder> app)
        {
            var fakeKlrServiceProvider = new ServiceCollection()
                                             .AddSingleton<IApplicationEnvironment, EmbeddedApplicationEnvironment>()
                                             .BuildServiceProvider();

            return Create(fakeKlrServiceProvider, app);
        }

        public static EmbeddedServer Create(IServiceProvider provider, Action<IBuilder> app)
        {
            var collection = new ServiceCollection();
            var hostingServices = HostingServices.GetDefaultServices();

            var config = new Configuration();
            collection.Add(hostingServices);

            var serviceProvider = collection.BuildServiceProvider(provider);
            return new EmbeddedServer(config, serviceProvider, app);
        }

        public EmbeddedServer(IConfiguration config, IServiceProvider serviceProvider, Action<IBuilder> appStartup)
        {
            var env = serviceProvider.GetService<IApplicationEnvironment>();
            if (env == null)
            {
                throw new InvalidOperationException("The service provider must be able to resolve an IApplicationEnvironment");
            }

            HostingContext hostContext = new HostingContext()
            {
                ApplicationName = env.ApplicationName,
                Configuration = config,
                ServerFactory = this,
                Services = serviceProvider,
                ApplicationStartup = appStartup
            };

            var engine = serviceProvider.GetService<IHostingEngine>();
            var disposable = engine.Start(hostContext);
        }

        public IServerInformation Initialize(IConfiguration configuration)
        {
            return new ServerInformation();
        }

        public IDisposable Start(IServerInformation serverInformation, Func<object, Task> application)
        {
            if (!serverInformation.GetType().Equals(typeof(ServerInformation)))
            {
                throw new ArgumentException("serverInformation");
            }

            _appDelegate = application;

            return this;
        }

        public EmbeddedClient Handler { get { return new EmbeddedClient(_appDelegate); } }

        public void Dispose()
        {
            // No op
        }

        private class ServerInformation : IServerInformation
        {
            public string Name
            {
                get { return EmbeddedServer.ServerName; }
            }
        }

        private class EmbeddedApplicationEnvironment : IApplicationEnvironment
        {
            public EmbeddedApplicationEnvironment()
            {
                var assemblyName = typeof(EmbeddedApplicationEnvironment).Assembly.GetName();
                ApplicationBasePath = Environment.CurrentDirectory;
                ApplicationName = assemblyName.Name;
                Version = assemblyName.Version.ToString();
                TargetFramework = new FrameworkName(".NETFramework", new Version(4, 5));
            }
            public string ApplicationName { get; set; }

            public string Version { get; set; }

            public string ApplicationBasePath { get; set; }

            public FrameworkName TargetFramework { get; set; }
        }
    }
}
