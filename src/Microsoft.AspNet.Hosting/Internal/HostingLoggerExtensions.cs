﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Hosting.Internal
{
    internal static class HostingLoggerExtensions
    {
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        public static IDisposable RequestScope(this ILogger logger, HttpContext httpContext)
        {
            return logger.BeginScopeImpl(new HostingLogScope(httpContext));
        }

        public static void RequestStarting(this ILogger logger, HttpContext httpContext)
        {
            var requestStarting = new HostingRequestStarting(httpContext);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(
                    logLevel: LogLevel.Information,
                    eventId: LoggerEventIds.RequestStarting,
                    state: requestStarting,
                    exception: null,
                    formatter: HostingRequestStarting.MessageFormatter);
            }
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(
                    logLevel: LogLevel.Debug,
                    eventId: LoggerEventIds.RequestStarting,
                    state: requestStarting,
                    exception: null,
                    formatter: HostingRequestStarting.HeaderFormatter);
            }
        }

        public static void RequestFinished(this ILogger logger, HttpContext httpContext, long startTimestamp, long currentTimestamp)
        {
            // Don't log if Information logging wasn't enabled at start or end of request as time will be wildly wrong.
            if (startTimestamp != 0)
            {
                var elapsed = new TimeSpan((long)(TimestampToTicks * (currentTimestamp - startTimestamp)));
                var requestFinished = new HostingRequestFinished(httpContext, elapsed);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.Log(
                        logLevel: LogLevel.Information,
                        eventId: LoggerEventIds.RequestFinished,
                        state: requestFinished,
                        exception: null,
                        formatter: HostingRequestFinished.MessageFormatter);
                }
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.Log(
                        logLevel: LogLevel.Debug,
                        eventId: LoggerEventIds.RequestFinished,
                        state: requestFinished,
                        exception: null,
                        formatter: HostingRequestFinished.HeaderFormatter);
                }
            }
        }

        public static void ApplicationError(this ILogger logger, Exception exception)
        {
            logger.LogError(
                eventId: LoggerEventIds.ApplicationStartupException,
                message: "Application startup exception",
                error: exception);
        }

        public static void Starting(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                   eventId: LoggerEventIds.Starting,
                   data: "Hosting starting");
            }
        }

        public static void Started(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    eventId: LoggerEventIds.Started,
                    data: "Hosting started");
            }
        }

        public static void Shutdown(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    eventId: LoggerEventIds.Shutdown,
                    data: "Hosting shutdown");
            }
        }


        private class HostingLogScope : ILogValues
        {
            private readonly HttpContext _httpContext;

            private string _cachedToString;
            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;

            public HostingLogScope(HttpContext httpContext)
            {
                _httpContext = httpContext;
            }

            public override string ToString()
            {
                if (_cachedToString == null)
                {
                    _cachedToString = $"RequestId:{_httpContext.TraceIdentifier} RequestPath:{_httpContext.Request.Path}";
                }

                return _cachedToString;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues()
            {
                if (_cachedGetValues == null)
                {
                    _cachedGetValues = new[]
                    {
                        new KeyValuePair<string, object>("RequestId", _httpContext.TraceIdentifier),
                        new KeyValuePair<string, object>("RequestPath", _httpContext.Request.Path.ToString()),
                    };
                }

                return _cachedGetValues;
            }
        }

        private class HostingRequestStarting : ILogValues
        {
            internal static readonly Func<object, Exception, string> MessageFormatter = (state, exception) => ((HostingRequestStarting)state).ToString();
            internal static readonly Func<object, Exception, string> HeaderFormatter = (state, exception) => ((HostingRequestStarting)state).GetHeaderString();

            private readonly HttpRequest _request;

            private string _cachedMessageString;
            private string _cachedHeaderString;
            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;

            public HostingRequestStarting(HttpContext httpContext)
            {
                _request = httpContext.Request;
            }

            public override string ToString()
            {
                if (_cachedMessageString == null)
                {
                    _cachedMessageString = $"Request starting {_request.Protocol} {_request.Method} {_request.Scheme}://{_request.Host}{_request.PathBase}{_request.Path}{_request.QueryString} {_request.ContentType} {_request.ContentLength}";
                }
                return _cachedMessageString;
            }

            public string GetHeaderString()
            {
                if (_cachedHeaderString == null)
                {
                    var stringBuilder = new StringBuilder($"Request Headers:{Environment.NewLine}");
                    stringBuilder.AppendLine("{");
                    foreach (var header in _request.Headers)
                    {
                        foreach (var value in header.Value)
                        {
                            stringBuilder.AppendLine($"    {header.Key}: {value}; ");
                        }
                    }
                    stringBuilder.AppendLine("}");
                    _cachedHeaderString = stringBuilder.ToString();
                }
                return _cachedHeaderString;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues()
            {
                if (_cachedGetValues == null)
                {
                    _cachedGetValues = new[]
                    {
                        new KeyValuePair<string, object>("Protocol", _request.Protocol),
                        new KeyValuePair<string, object>("Method", _request.Method),
                        new KeyValuePair<string, object>("ContentType", _request.ContentType),
                        new KeyValuePair<string, object>("ContentLength", _request.ContentLength),
                        new KeyValuePair<string, object>("Scheme", _request.Scheme.ToString()),
                        new KeyValuePair<string, object>("Host", _request.Host.ToString()),
                        new KeyValuePair<string, object>("PathBase", _request.PathBase.ToString()),
                        new KeyValuePair<string, object>("Path", _request.Path.ToString()),
                        new KeyValuePair<string, object>("QueryString", _request.QueryString.ToString()),
                    };
                }

                return _cachedGetValues;
            }
        }

        private class HostingRequestFinished
        {
            internal static readonly Func<object, Exception, string> MessageFormatter = (state, exception) => ((HostingRequestFinished)state).ToString();
            internal static readonly Func<object, Exception, string> HeaderFormatter = (state, exception) => ((HostingRequestFinished)state).GetHeaderString();

            private readonly HttpResponse _response;
            private readonly TimeSpan _elapsed;

            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;
            private string _cachedMessageString;
            private string _cachedHeaderString;

            public HostingRequestFinished(HttpContext httpContext, TimeSpan elapsed)
            {
                _response = httpContext.Response;
                _elapsed = elapsed;
            }

            public override string ToString()
            {
                if (_cachedMessageString == null)
                {
                    _cachedMessageString = $"Request finished in {_elapsed.TotalMilliseconds}ms {_response.StatusCode} {_response.ContentType}";
                }
                return _cachedMessageString;
            }

            public string GetHeaderString()
            {
                if (_cachedHeaderString == null)
                {
                    var stringBuilder = new StringBuilder($"Response Headers:{Environment.NewLine}");

                    stringBuilder.AppendLine("{");
                    foreach (var header in _response.Headers)
                    {
                        foreach (var value in header.Value)
                        {
                            stringBuilder.AppendLine($"    {header.Key}: {value};");
                        }
                    }
                    stringBuilder.AppendLine("}");
                    _cachedHeaderString = stringBuilder.ToString();
                }
                return _cachedHeaderString;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues()
            {
                if (_cachedGetValues == null)
                {
                    _cachedGetValues = new[]
                    {
                        new KeyValuePair<string, object>("ElapsedMilliseconds", _elapsed.TotalMilliseconds),
                        new KeyValuePair<string, object>("StatusCode", _response.StatusCode),
                        new KeyValuePair<string, object>("ContentType", _response.ContentType),
                    };
                }

                return _cachedGetValues;
            }
        }
    }
}

