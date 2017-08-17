﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Internal;
using Moq;
using Xunit;
namespace Microsoft.AspNetCore.Hosting.Tests.Internal
{
    public class HostingRequestStartLogTests
    {
        [Theory]
        [InlineData(",XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX", "GET 1.1 http://,XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX//?query test 0")]
        [InlineData(" XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX", "GET 1.1 http:// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX//?query test 0")]
        public void InvalidHttpContext_DoesNotThrowOnAccessingProperties(string input, string expected)
        {
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(request => request.Protocol).Returns("GET");
            mockRequest.Setup(request => request.Method).Returns("1.1");
            mockRequest.Setup(request => request.Scheme).Returns("http");
            mockRequest.Setup(request => request.Host).Returns(new HostString(input));
            mockRequest.Setup(request => request.PathBase).Returns(new PathString("/"));
            mockRequest.Setup(request => request.Path).Returns(new PathString("/"));
            mockRequest.Setup(request => request.QueryString).Returns(new QueryString("?query"));
            mockRequest.Setup(request => request.ContentType).Returns("test");
            mockRequest.Setup(request => request.ContentLength).Returns(0);

            var mockContext = new Mock<HttpContext>();
            mockContext.Setup(context => context.Request).Returns(mockRequest.Object);

            var logger = new HostingRequestStartingLog(mockContext.Object);
            Assert.Equal(expected, logger.ToString());
        }
    }
}
