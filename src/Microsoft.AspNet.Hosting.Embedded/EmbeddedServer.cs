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

        public EmbeddedServer(IConfiguration config, IServiceProvider serviceProvider, Action<IBuilder> appStartup)
        {
            var env = serviceProvider.GetService<IApplicationEnvironment>();
            if (env == null)
            {
                throw new ArgumentException("IApplicationEnvironment couldn't be resolved.", "serviceProvider");
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

        public static EmbeddedServer Create<TStartup>(IServiceProvider provider)
        {
            var startupLoader = new StartupLoader(provider, new NullStartupLoader());
            var name = typeof(TStartup).AssemblyQualifiedName;
            var diagnosticMessages = new List<string>();
            return Create(provider, startupLoader.LoadStartup(name, diagnosticMessages));
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

        public EmbeddedClient Handler { get { return new EmbeddedClient(_appDelegate); } }

        public IServerInformation Initialize(IConfiguration configuration)
        {
            return new ServerInformation();
        }

        public IDisposable Start(IServerInformation serverInformation, Func<object, Task> application)
        {
            if (!serverInformation.GetType().Equals(typeof(ServerInformation)))
            {
                throw new ArgumentException(string.Format("The server must be {0}", ServerName), "serverInformation");
            }

            _appDelegate = application;

            return this;
        }

        public void Dispose()
        {
            // IServerFactory.Start needs to return an IDisposable. Tipically this IDisposable instance is used to 
            // clear any server resources when tearing down the host. In our case we don't have anything to clear
            // so we just implement IDisposable and do nothing.
        }

        private class ServerInformation : IServerInformation
        {
            public string Name
            {
                get { return EmbeddedServer.ServerName; }
            }
        }
    }
}
