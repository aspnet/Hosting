using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Net.Runtime;
using Microsoft.AspNet.HttpFeature;
using System.IO;
using System.Runtime.Versioning;
using System.Diagnostics;

namespace Microsoft.AspNet.Hosting.Testing
{
    public class TestServer : IServerFactory, IDisposable
    {
        private static readonly string ServerName = "Microsoft.AspNet.Host.Testing";
        private Func<object, Task> _appDelegate = null;

        public static TestServer Create<TStartup>()
        {
            var fakeKlrServiceProvider = new ServiceCollection()
                                 .AddSingleton<IApplicationEnvironment, TestApplicationEnvironment>()
                                 .BuildServiceProvider();

            return Create<TStartup>(fakeKlrServiceProvider);
        }

        public static TestServer Create<TStartup>(IServiceProvider provider)
        {
            var startupLoader = new StartupLoader(provider, new NullStartupLoader());
            var name = typeof(TStartup).AssemblyQualifiedName;
            var diagnosticMessages = new List<string>();
            return Create(provider, startupLoader.LoadStartup(name, diagnosticMessages));
        }

        public static TestServer Create(Action<IBuilder> app)
        {
            var fakeKlrServiceProvider = new ServiceCollection()
                                             .AddSingleton<IApplicationEnvironment, TestApplicationEnvironment>()
                                             .BuildServiceProvider();

            return Create(fakeKlrServiceProvider, app);
        }

        public static TestServer Create(IServiceProvider provider, Action<IBuilder> app)
        {
            var collection = new ServiceCollection();
            var hostingServices = HostingServices.GetDefaultServices();

            var config = new Configuration();
            collection.Add(hostingServices);

            var serviceProvider = collection.BuildServiceProvider(provider);
            return new TestServer(config, serviceProvider, app);
        }

        public TestServer(IConfiguration config, IServiceProvider serviceProvider, Action<IBuilder> appStartup)
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

        public TestClient Handler { get { return new TestClient(_appDelegate); } }

        public void Dispose()
        {
            // No op
        }

        private class ServerInformation : IServerInformation
        {
            public string Name
            {
                get { return TestServer.ServerName; }
            }
        }

        private class TestApplicationEnvironment : IApplicationEnvironment
        {
            public string ApplicationName
            {
                get { return "Microsoft.AspNet.Host.Testing"; }
            }

            public string Version
            {
                get { return "0.1-alpha"; }
            }

            public string ApplicationBasePath
            {
                get { return Environment.CurrentDirectory; }
            }

            public FrameworkName TargetFramework
            {
                get { return new FrameworkName(".NET Framework", new Version("4.5")); }
            }
        }
    }
}
