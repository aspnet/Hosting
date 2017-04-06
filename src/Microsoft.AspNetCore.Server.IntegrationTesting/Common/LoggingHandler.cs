﻿using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    internal class LoggingHandler : DelegatingHandler
    {
        private ILogger _logger;

        public LoggingHandler(ILoggerFactory loggerFactory, HttpMessageHandler innerHandler) : base(innerHandler)
        {
            _logger = loggerFactory.CreateLogger<HttpClient>();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Sending {method} {url}", request.Method, request.RequestUri);
            var response = await base.SendAsync(request, cancellationToken);
            _logger.LogDebug("Received {statusCode} {reasonPhrase} {url}", response.StatusCode, response.ReasonPhrase, request.RequestUri);
            return response;
        }
    }
}