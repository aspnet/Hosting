// Copyright (c) .NET Foundation. All rights reserved.
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
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(
                    logLevel: LogLevel.Debug,
                    eventId: LoggerEventIds.RequestStarting,
                    state: new HostingRequestStarting(httpContext, LogLevel.Debug),
                    exception: null,
                    formatter: HostingRequestStarting.Callback);
            }
            else if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(
                    logLevel: LogLevel.Information,
                    eventId: LoggerEventIds.RequestStarting,
                    state: new HostingRequestStarting(httpContext, LogLevel.Information),
                    exception: null,
                    formatter: HostingRequestStarting.Callback);
            }
        }

        public static void RequestFinished(this ILogger logger, HttpContext httpContext, long startTimestamp, long currentTimestamp)
        {
            // Don't log if Information logging wasn't enabled at start or end of request as time will be wildly wrong.
            if (startTimestamp != 0)
            {
                var elapsed = new TimeSpan((long)(TimestampToTicks * (currentTimestamp - startTimestamp)));

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.Log(
                        logLevel: LogLevel.Debug,
                        eventId: LoggerEventIds.RequestFinished,
                        state: new HostingRequestFinished(httpContext, LogLevel.Debug, elapsed),
                        exception: null,
                        formatter: HostingRequestFinished.Callback);
                }
                else if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.Log(
                        logLevel: LogLevel.Information,
                        eventId: LoggerEventIds.RequestFinished,
                        state: new HostingRequestFinished(httpContext, LogLevel.Information, elapsed),
                        exception: null,
                        formatter: HostingRequestFinished.Callback);
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
            internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestStarting)state).ToString();

            private readonly HttpRequest _request;
            private readonly LogLevel _logLevel;

            private string _cachedToString;
            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;

            public HostingRequestStarting(HttpContext httpContext, LogLevel logLevel)
            {
                _request = httpContext.Request;
                _logLevel = logLevel;
            }

            public override string ToString()
            {
                if (_cachedToString == null)
                {
                    if (_logLevel <= LogLevel.Debug)
                    {
                        var stringBuilder = new StringBuilder($"Request starting {_request.Protocol} {_request.Method} {_request.Scheme}://{_request.Host}{_request.PathBase}{_request.Path}{_request.QueryString} {_request.ContentType} {_request.ContentLength}");

                        stringBuilder.Append("; Headers: { ");
                        foreach (var header in _request.Headers)
                        {
                            foreach (var value in header.Value)
                            {
                                stringBuilder.Append(header.Key);
                                stringBuilder.Append(": ");
                                stringBuilder.Append(value);
                                stringBuilder.Append("; ");
                            }
                        }
                        stringBuilder.Append("}");
                        _cachedToString = stringBuilder.ToString();
                    }
                    else
                    {
                        _cachedToString = $"Request starting {_request.Protocol} {_request.Method} {_request.Scheme}://{_request.Host}{_request.PathBase}{_request.Path}{_request.QueryString} {_request.ContentType} {_request.ContentLength}";
                    }
                }

                return _cachedToString;
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
            internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestFinished)state).ToString();

            private readonly HttpResponse _response;
            private readonly LogLevel _logLevel;
            private readonly TimeSpan _elapsed;

            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;
            private string _cachedToString;

            public HostingRequestFinished(HttpContext httpContext, LogLevel logLevel, TimeSpan elapsed)
            {
                _response = httpContext.Response;
                _logLevel = logLevel;
                _elapsed = elapsed;
            }

            public override string ToString()
            {
                if (_cachedToString == null)
                {
                    if (_logLevel <= LogLevel.Debug)
                    {
                        var stringBuilder = new StringBuilder($"Request finished in {_elapsed.TotalMilliseconds}ms {_response.StatusCode} {_response.ContentType}");

                        stringBuilder.Append("; Headers: { ");
                        foreach (var header in _response.Headers)
                        {
                            foreach (var value in header.Value)
                            {
                                stringBuilder.Append(header.Key);
                                stringBuilder.Append(": ");
                                stringBuilder.Append(value);
                                stringBuilder.Append("; ");
                            }
                        }
                        stringBuilder.Append("}");
                        _cachedToString = stringBuilder.ToString();
                    }
                    else
                    {
                        _cachedToString = $"Request finished in {_elapsed.TotalMilliseconds}ms {_response.StatusCode} {_response.ContentType}";
                    }
                }

                return _cachedToString;
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

