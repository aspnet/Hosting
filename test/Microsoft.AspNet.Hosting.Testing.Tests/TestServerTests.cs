using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Xunit;
using Microsoft.AspNet.Abstractions;
using System.IO;
using Microsoft.Net.Runtime;
using System.Runtime.Versioning;

namespace Microsoft.AspNet.Hosting.Testing.Tests
{
    public class TestServerTests
    {
        [Fact]
        public void TestServer_CreateWithDelegate()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => TestServer.Create(app => { }));
        }

        [Fact]
        public void TestServer_CreateWithCustomServiceProviderAndDelegate()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddSingleton<IApplicationEnvironment, MyAppEnvironment>()
                .BuildServiceProvider();

            // Act & Assert
            Assert.DoesNotThrow(() => TestServer.Create(services, app => { }));
        }

        [Fact]
        public async Task TestServer_CreateWithGeneric()
        {
            // Arrange
            var server = TestServer.Create<Startup>();
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://any");

            // Assert
            Assert.Equal("Startup", new StreamReader(response.Body).ReadToEnd());
        }
    }

    public class MyAppEnvironment : IApplicationEnvironment
    {

        public string ApplicationName
        {
            get { return "Hello world"; }
        }

        public string Version
        {
            get { return "1.0.0.0"; }
        }

        public string ApplicationBasePath
        {
            get { return "."; }
        }

        public FrameworkName TargetFramework
        {
            get { return new FrameworkName(".NET Framework", new Version("4.5")); }
        }
    }

    public class Startup
    {
        public void Configuration(IBuilder builder)
        {
            builder.Run(ctx => ctx.Response.WriteAsync("Startup"));
        }
    }

    public class AnotherStartup
    {
        public void Configuration(IBuilder builder)
        {
            builder.Run(ctx => ctx.Response.WriteAsync("Another Startup"));
        }
    }
}
