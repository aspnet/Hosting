// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Xunit;
using System.Threading;
using System.Net.WebSockets;

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
        public async Task WebSocketWorks()
        {
            // Arrange
            RequestDelegate appDelegate = async ctx =>
            {
                if (ctx.WebSockets.IsWebSocketRequest)
                {
                    var websocket = await ctx.WebSockets.AcceptWebSocketAsync();
                    await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal Closure", CancellationToken.None);
                }
            };
            var server = TestServer.Create(app =>
            {
                app.UseWebSockets();
                app.Run(appDelegate);
            });

            // Act
            var client = server.CreateWebSocketClient();
            var clientSocket = await client.ConnectAsync(new System.Uri("http://localhost"), CancellationToken.None);
            byte[] buffer = new byte[0];
            var msg = await clientSocket.ReceiveAsync(new System.ArraySegment<byte>(buffer), CancellationToken.None);
            await clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal Closure", CancellationToken.None);

            // Assert
            Assert.Equal(WebSocketMessageType.Close, msg.MessageType);
            Assert.Equal(WebSocketState.Closed, clientSocket.State);
        }
    }
}
