using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.Net.Runtime;
using Xunit;

namespace Microsoft.AspNet.Hosting.Embedded
{
    public class TestServerTests
    {
        [Fact]
        public void TestServer_CreateWithDelegate()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => EmbeddedServer.Create(app => { }));
        }

        [Fact]
        public void TestServer_CreateWithCustomServiceProviderAndDelegate()
        {
            // Arrange
            var services = new ServiceCollection()
                .AddSingleton<IApplicationEnvironment, MyAppEnvironment>()
                .BuildServiceProvider();

            // Act & Assert
            Assert.DoesNotThrow(() => EmbeddedServer.Create(services, app => { }));
        }

        [Fact]
        public async Task TestServer_CreateWithGeneric()
        {
            // Arrange
            var server = EmbeddedServer.Create<Startup>();
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
