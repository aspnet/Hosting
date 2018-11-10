using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.Tests
{
    public class GenericHostBuilderTests
    {
        [Fact]
        public void StartupErrorsAreLoggedIfCaptureStartupErrorsIsFalse()
        {
            var host = new HostBuilder()
                .ConfigureWebHost(builder =>
                {
                    builder.CaptureStartupErrors(false)
                           .Configure(app =>
                           {
                               throw new InvalidOperationException("Startup exception");
                           })
                           .UseServer(new TestServer());
                })
                .Build();

            Assert.Throws<InvalidOperationException>(() => host.Start());
        }

        [Fact]
        public void HostingStartupFromPrimaryAssemblyCanBeDisabled()
        {
            var host = new HostBuilder()
                .ConfigureWebHost(builder =>
                {
                    builder.UseSetting(WebHostDefaults.PreventHostingStartupKey, "true")
                    .Configure(app => { })
                    .UseServer(new TestServer());
                })
                .Build();

            using (host)
            {
                var config = host.Services.GetRequiredService<IConfiguration>();
                Assert.Null(config["testhostingstartup"]);
            }
        }

        [Fact]
        public async Task Build_DoesNotThrowIfUnloadableAssemblyNameInHostingStartupAssembliesAndCaptureStartupErrorsTrue()
        {
            var provider = new TestLoggerProvider();
            var host = new HostBuilder()
                .ConfigureWebHost(builder =>
                {
                    builder.ConfigureLogging((_, factory) =>
                    {
                        factory.AddProvider(provider);
                    })
                    .CaptureStartupErrors(true)
                    .UseSetting(WebHostDefaults.HostingStartupAssembliesKey, "SomeBogusName")
                    .Configure(app => { })
                    .UseServer(new TestServer());
                })
                .Build();

            using (host)
            {
                await host.StartAsync();
                var context = provider.Sink.Writes.FirstOrDefault(s => s.EventId.Id == LoggerEventIds.HostingStartupAssemblyException);
                Assert.NotNull(context);
            }
        }

        public class TestLoggerProvider : ILoggerProvider
        {
            public TestSink Sink { get; set; } = new TestSink();

            public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName, Sink, enabled: true);

            public void Dispose() { }
        }

        private class TestServer : IServer
        {
            IFeatureCollection IServer.Features { get; }
            public RequestDelegate RequestDelegate { get; private set; }

            public void Dispose() { }

            public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
            {
                RequestDelegate = async ctx =>
                {
                    var httpContext = application.CreateContext(ctx.Features);
                    try
                    {
                        await application.ProcessRequestAsync(httpContext);
                    }
                    catch (Exception ex)
                    {
                        application.DisposeContext(httpContext, ex);
                        throw;
                    }
                    application.DisposeContext(httpContext, null);
                };

                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
