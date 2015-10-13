// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Hosting.Internal
{
    internal static class HostingLoggerExtensions
    {
        public static IDisposable RequestScope(this ILogger logger, HttpContext httpContext)
        {
            return logger.BeginScopeImpl(new HostingRequestScope(httpContext));
        }

        public static void RequestStarting(this ILogger logger, HttpContext httpContext)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, 1, new HostingRequestStarting(httpContext), null, HostingRequestStarting.Callback);
            }
        }

        public static void RequestFinished(this ILogger logger, HttpContext httpContext)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, 2, new HostingRequestFinished(httpContext), null, HostingRequestFinished.Callback);
            }
        }

        private class HostingRequestScope : ILogValues
        {
            private readonly HttpContext _httpContext;

            private FeatureReference<IHttpRequestIdentifierFeature> _requestIdentifierFeatureReference;
            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;
            private string _cachedToString;

            public HostingRequestScope(HttpContext httpContext)
            {
                this._httpContext = httpContext;
            }

            public IHttpRequestIdentifierFeature RequestIdFeature =>
                _requestIdentifierFeatureReference.Fetch(_httpContext.Features) ??
                _requestIdentifierFeatureReference.Update(_httpContext.Features, new FastHttpRequestIdentifierFeature());

            public override string ToString() => _cachedToString ?? Interlocked.CompareExchange(
                ref _cachedToString,
                $"RequestId:{RequestIdFeature.TraceIdentifier} RequestPath:{_httpContext.Request.Path}",
                null);

            public IEnumerable<KeyValuePair<string, object>> GetValues() => _cachedGetValues ?? Interlocked.CompareExchange(
                ref _cachedGetValues,
                new[]
                {
                    new KeyValuePair<string, object>("RequestId", RequestIdFeature.TraceIdentifier),
                    new KeyValuePair<string, object>("RequestPath", _httpContext.Request.Path.ToString()),
                },
                null);
        }

        private class HostingRequestStarting : ILogValues
        {
            internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestStarting)state).ToString();

            private readonly HttpContext _httpContext;
            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;
            private string _cachedToString;

            public HostingRequestStarting(HttpContext httpContext)
            {
                _httpContext = httpContext;
            }

            public override string ToString() => _cachedToString ?? Interlocked.CompareExchange(
                ref _cachedToString,
                $"Request starting {_httpContext.Request.Protocol} {_httpContext.Request.Method} {_httpContext.Request.Scheme}://{_httpContext.Request.Host}{_httpContext.Request.PathBase}{_httpContext.Request.Path}{_httpContext.Request.QueryString} {_httpContext.Request.ContentType} {_httpContext.Request.ContentLength}",
                null);

            public IEnumerable<KeyValuePair<string, object>> GetValues() => _cachedGetValues ?? Interlocked.CompareExchange(
                ref _cachedGetValues,
                new[]
                {
                    new KeyValuePair<string, object>("EventName", "RequestStarting"),
                    new KeyValuePair<string, object>("Protocol", _httpContext.Request.Protocol),
                    new KeyValuePair<string, object>("Method", _httpContext.Request.Method),
                    new KeyValuePair<string, object>("ContentType", _httpContext.Request.ContentType),
                    new KeyValuePair<string, object>("ContentLength", _httpContext.Request.ContentLength),
                    new KeyValuePair<string, object>("Scheme", _httpContext.Request.Scheme.ToString()),
                    new KeyValuePair<string, object>("Host", _httpContext.Request.Host.ToString()),
                    new KeyValuePair<string, object>("PathBase", _httpContext.Request.PathBase.ToString()),
                    new KeyValuePair<string, object>("Path", _httpContext.Request.Path.ToString()),
                    new KeyValuePair<string, object>("QueryString", _httpContext.Request.QueryString.ToString()),
                },
                null);
        }

        private class HostingRequestFinished
        {
            internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestFinished)state).ToString();

            private readonly HttpContext _httpContext;
            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;
            private string _cachedToString;

            public HostingRequestFinished(HttpContext httpContext)
            {
                _httpContext = httpContext;
            }

            public override string ToString() => _cachedToString ?? Interlocked.CompareExchange(
                ref _cachedToString,
                $"Request finished {_httpContext.Response.StatusCode} {_httpContext.Response.ContentType} {_httpContext.Response.Body.Length}",
                null);

            public IEnumerable<KeyValuePair<string, object>> GetValues() => _cachedGetValues ?? Interlocked.CompareExchange(
                ref _cachedGetValues,
                new[]
                {
                    new KeyValuePair<string, object>("EventName", "RequestFinished"),
                    new KeyValuePair<string, object>("StatusCode", _httpContext.Response.StatusCode),
                    new KeyValuePair<string, object>("ContentType", _httpContext.Response.ContentType),
                    new KeyValuePair<string, object>("BodyLength", _httpContext.Response.Body.Length),
                },
                null);
        }
    }
}

