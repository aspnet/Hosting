using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
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
