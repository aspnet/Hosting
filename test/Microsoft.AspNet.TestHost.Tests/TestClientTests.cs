// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Xunit;

namespace Microsoft.AspNet.TestHost
{
    public class TestClientTests
    {
        private readonly TestServer _server;

        public TestClientTests()
        {
            _server = TestServer.Create(app => app.Run(ctx => Task.FromResult(0)));
        }

        [Fact]
        public async Task GetAsyncWorks()
        {
            // Arrange
            var expected = "GET Response";
            RequestDelegate appDelegate = ctx =>
                ctx.Response.WriteAsync(expected);
            var server = TestServer.Create(app => app.Run(appDelegate));
            var client = server.CreateClient();

            // Act
            var actual = await client.GetStringAsync("http://localhost:12345");

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task NoTrailingSlash_NoPathBase()
        {
            // Arrange
            var expected = "GET Response";
            RequestDelegate appDelegate = ctx =>
            {
                Assert.Equal("", ctx.Request.PathBase.Value);
                Assert.Equal("/", ctx.Request.Path.Value);
                return ctx.Response.WriteAsync(expected);
            };
            var server = TestServer.Create(app => app.Run(appDelegate));
            var client = server.CreateClient();

            // Act
            var actual = await client.GetStringAsync("http://localhost:12345");

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task SingleTrailingSlash_NoPathBase()
        {
            // Arrange
            var expected = "GET Response";
            RequestDelegate appDelegate = ctx =>
            {
                Assert.Equal("", ctx.Request.PathBase.Value);
                Assert.Equal("/", ctx.Request.Path.Value);
                return ctx.Response.WriteAsync(expected);
            };
            var server = TestServer.Create(app => app.Run(appDelegate));
            var client = server.CreateClient();

            // Act
            var actual = await client.GetStringAsync("http://localhost:12345/");

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task PutAsyncWorks()
        {
            // Arrange
            RequestDelegate appDelegate = ctx =>
                ctx.Response.WriteAsync(new StreamReader(ctx.Request.Body).ReadToEnd() + " PUT Response");
            var server = TestServer.Create(app => app.Run(appDelegate));
            var client = server.CreateClient();

            // Act
            var content = new StringContent("Hello world");
            var response = await client.PutAsync("http://localhost:12345", content);

            // Assert
            Assert.Equal("Hello world PUT Response", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task PostAsyncWorks()
        {
            // Arrange
            RequestDelegate appDelegate = async ctx =>
                await ctx.Response.WriteAsync(new StreamReader(ctx.Request.Body).ReadToEnd() + " POST Response");
            var server = TestServer.Create(app => app.Run(appDelegate));
            var client = server.CreateClient();

            // Act
            var content = new StringContent("Hello world");
            var response = await client.PostAsync("http://localhost:12345", content);

            // Assert
            Assert.Equal("Hello world POST Response", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ClientDisposalAbortsRequest()
        {
            // Arrange
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            RequestDelegate appDelegate = async ctx =>
            {
                // Write Headers
                await ctx.Response.Body.FlushAsync();

                var sem = new SemaphoreSlim(0);
                try
                {
                    await sem.WaitAsync(ctx.RequestAborted);
                }
                catch(Exception e)
                {
                    tcs.SetException(e);
                }
            };

            // Act
            var server = TestServer.Create(app => app.Run(appDelegate));
            var client = server.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:12345");
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            // Abort Request
            response.Dispose();

            // Assert
            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await tcs.Task);
        }

        [Fact]
        public async Task ClientCancellationAbortsRequest()
        {
            // Arrange
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            RequestDelegate appDelegate = async ctx =>
            {
                var sem = new SemaphoreSlim(0);
                try
                {
                    await sem.WaitAsync(ctx.RequestAborted);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            };

            // Act
            var server = TestServer.Create(app => app.Run(appDelegate));
            var client = server.CreateClient();
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var response = await client.GetAsync("http://localhost:12345", cts.Token);

            // Assert
            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await tcs.Task);
        }
    }
}
