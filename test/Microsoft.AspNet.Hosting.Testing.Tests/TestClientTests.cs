using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Xunit;
using System.Diagnostics;

namespace Microsoft.AspNet.Hosting.Testing.Tests
{
    public class TestClientTests
    {
        TestServer _server = TestServer.Create(app => app.Run(async ctx => { }));

        [Fact]
        public async Task TestClient_SendAsync_ConfiguresRequestProperly()
        {
            // Arrange
            var client = _server.Handler;

            // Act
            var response = await client.SendAsync("GET", "http://localhost:12345/Home/Index?id=3&name=peter#fragment");
            var request = response.HttpContext.Request;

            // Assert
            Assert.NotNull(request);
            Assert.Equal("HTTP/1.1", request.Protocol);
            Assert.Equal("GET", request.Method);
            Assert.Equal("http", request.Scheme);
            Assert.Equal("localhost:12345", request.Host.Value);
            Assert.Equal("", request.PathBase.Value);
            Assert.True(request.Path.HasValue);
            Assert.Equal("/Home/Index", request.Path.Value);
            Assert.Equal("?id=3&name=peter", request.QueryString.Value);
            Assert.Null(request.ContentLength);
            Assert.Equal(1, request.Headers.Count);
            Assert.True(request.Headers.ContainsKey("Host"));
        }

        [Fact]
        public async Task TestClient_SendAsync_InvokesCallbackWhenPassed()
        {
            // Arrange
            var client = _server.Handler;
            var invoked = false;

            // Act
            var response = await client.SendAsync("GET", "http://localhost:12345/", null, null, _ => invoked = true);

            // Assert
            Assert.True(invoked);
        }

        [Fact]
        public async Task TestClient_SendAsync_RespectsExistingHost()
        {
            // Arrange
            var client = _server.Handler;
            var headers = new Dictionary<string, string[]> { { "Host", new string[] { "server:12345" } } };

            // Act
            var response = await client.SendAsync("GET", "http://localhost:12345/Home/", headers);
            var request = response.HttpContext.Request;

            // Assert
            Assert.Equal("server:12345", request.Host.Value);
        }

        [Fact]
        public async Task TestClient_SendAsync_RespectsArgumentBody()
        {
            // Arrange
            var client = _server.Handler;
            var headers = new Dictionary<string, string[]> { { "Content-Type", new string[] { "text/plain" } } };
            var body = new MemoryStream();
            new StreamWriter(body).Write("Hello world");
            body.Position = 0;

            // Act
            var response = await client.SendAsync("POST", "http://host/", headers, body);
            var request = response.HttpContext.Request;

            // Assert
            Assert.Same(body, request.Body);
            Assert.Equal(0, request.Body.Position);
            Assert.Equal(body.Length, request.ContentLength);
        }

        [Fact]
        public async Task TestClient_SendAsync_RewindsTheResponseStream()
        {
            // Arrange
            var server = TestServer.Create(app => app.Run(ctx => ctx.Response.WriteAsync("Hello world")));
            var client = server.Handler;

            // Act
            var response = await client.SendAsync("GET", "http://localhost");

            // Assert
            Assert.Equal(0, response.Body.Position);
            Assert.Equal("Hello world", new StreamReader(response.Body).ReadToEnd());
        }

        [Fact]
        public async Task TestClient_PutAsyncWorks()
        {
            // Arrange
            RequestDelegate appDelegate = async ctx =>
                ctx.Response.WriteAsync(new StreamReader(ctx.Request.Body).ReadToEnd());
            var server = TestServer.Create(app => app.Run(appDelegate));
            var client = server.Handler;

            // Act
            var response = await client.PutAsync("http://localhost:12345", "Hello world", "text/plain");
            var request = response.HttpContext.Request;

            // Assert
            Assert.Equal("PUT", request.Method);
            Assert.Equal("Hello world", new StreamReader(response.Body).ReadToEnd());
        }

        [Fact]
        public async Task TestClient_PostAsyncWorks()
        {
            // Arrange
            RequestDelegate appDelegate = async ctx =>
                ctx.Response.WriteAsync(new StreamReader(ctx.Request.Body).ReadToEnd());
            var server = TestServer.Create(app => app.Run(appDelegate));
            var client = server.Handler;

            // Act
            var response = await client.PostAsync("http://localhost:12345", "Hello world", "text/plain");
            var request = response.HttpContext.Request;

            // Assert
            Assert.Equal("POST", request.Method);
            Assert.Equal("Hello world", new StreamReader(response.Body).ReadToEnd());
        }

    }
}
