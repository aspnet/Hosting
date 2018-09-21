// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.Tests
{
    public class HostingApplicationTests
    {
        [Fact]
        public async Task DisposeContextDoesNotThrowWhenContextScopeIsNull()
        {
            // Arrange
            var app = CreateApplication();

            var context = new DefaultHttpContext();

            await app(context);
        }

        [Fact]
        public async Task CreateContextSetsCorrelationIdInScope()
        {
            // Arrange
            var logger = new LoggerWithScopes();
            var app = CreateApplication(logger: logger);
            var context = new DefaultHttpContext();
            context.Request.Headers["Request-Id"] = "some correlation id";

            // Act
            await app(context);

            // Assert
            Assert.Single(logger.Scopes);
            var pairs = ((IReadOnlyList<KeyValuePair<string, object>>)logger.Scopes[0]).ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal("some correlation id", pairs["CorrelationId"].ToString());
        }

        [Fact]
        public async Task ActivityIsNotCreatedWhenIsEnabledForActivityIsFalse()
        {
            var tcs = new TaskCompletionSource<object>();
            var diagnosticSource = new DiagnosticListener("DummySource");
            var middleware = CreateDiagnosticMiddleware(diagnosticSource: diagnosticSource);
            var context = new DefaultHttpContext();

            bool eventsFired = false;
            bool isEnabledActivityFired = false;
            bool isEnabledStartFired = false;

            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair =>
            {
                eventsFired |= pair.Key.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn");
            }), (s, o, arg3) =>
            {
                if (s == "Microsoft.AspNetCore.Hosting.HttpRequestIn")
                {
                    Assert.IsAssignableFrom<HttpContext>(o);
                    isEnabledActivityFired = true;
                }
                if (s == "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start")
                {
                    isEnabledStartFired = true;
                }
                return false;
            });

            var app = middleware(httpContext =>
            {
                Assert.Null(Activity.Current);
                return Task.CompletedTask;
            });

            await app(context);

            Assert.True(isEnabledActivityFired);
            Assert.False(isEnabledStartFired);
            Assert.False(eventsFired);
        }

        [Fact]
        public async Task ActivityIsCreatedButNotLoggedWhenIsEnabledForActivityStartIsFalse()
        {
            var diagnosticSource = new DiagnosticListener("DummySource");
            var middleware = CreateDiagnosticMiddleware(diagnosticSource: diagnosticSource);
            var context = new DefaultHttpContext();

            bool eventsFired = false;
            bool isEnabledStartFired = false;
            bool isEnabledActivityFired = false;

            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair =>
            {
                eventsFired |= pair.Key.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn");
            }), (s, o, arg3) =>
            {
                if (s == "Microsoft.AspNetCore.Hosting.HttpRequestIn")
                {
                    Assert.IsAssignableFrom<HttpContext>(o);
                    isEnabledActivityFired = true;
                    return true;
                }

                if (s == "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start")
                {
                    isEnabledStartFired = true;
                    return false;
                }
                return true;
            });

            var app = middleware(httpContext =>
            {
                Assert.NotNull(Activity.Current);
                return Task.CompletedTask;
            });

            await app(context);

            Assert.True(isEnabledActivityFired);
            Assert.True(isEnabledStartFired);
            Assert.False(eventsFired);
        }

        [Fact]
        public async Task ActivityIsCreatedAndLogged()
        {
            var diagnosticSource = new DiagnosticListener("DummySource");
            var middleware = CreateDiagnosticMiddleware(diagnosticSource: diagnosticSource);
            var context = new DefaultHttpContext();

            bool startCalled = false;

            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair =>
            {
                if (pair.Key == "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start")
                {
                    startCalled = true;
                    Assert.NotNull(pair.Value);
                    Assert.NotNull(Activity.Current);
                    Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
                    AssertProperty<HttpContext>(pair.Value, "HttpContext");
                }
            }));

            var app = middleware(httpContext =>
            {
                Assert.NotNull(Activity.Current);
                return Task.CompletedTask;
            });

            await app(context);

            Assert.True(startCalled);
        }

        [Fact]
        public async Task ActivityIsStoppedDuringStopCall()
        {
            var diagnosticSource = new DiagnosticListener("DummySource");
            var app = CreateApplication(diagnosticSource: diagnosticSource);
            var context = new DefaultHttpContext();

            bool endCalled = false;
            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair =>
            {
                if (pair.Key == "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop")
                {
                    endCalled = true;

                    Assert.NotNull(Activity.Current);
                    Assert.True(Activity.Current.Duration > TimeSpan.Zero);
                    Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
                    AssertProperty<HttpContext>(pair.Value, "HttpContext");
                }
            }));

            await app(context);

            Assert.True(endCalled);
        }

        [Fact]
        public async Task ActivityIsStoppedDuringUnhandledExceptionCall()
        {
            var diagnosticSource = new DiagnosticListener("DummySource");
            var middleware = CreateDiagnosticMiddleware(diagnosticSource: diagnosticSource);
            var context = new DefaultHttpContext();

            bool endCalled = false;
            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair =>
            {
                if (pair.Key == "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop")
                {
                    endCalled = true;
                    Assert.NotNull(Activity.Current);
                    Assert.True(Activity.Current.Duration > TimeSpan.Zero);
                    Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
                    AssertProperty<HttpContext>(pair.Value, "HttpContext");
                }
            }));

            var app = middleware(httpContext =>
            {
                throw new Exception();
            });

            await Assert.ThrowsAsync<Exception>(() => app(context));

            Assert.True(endCalled);
        }

        [Fact]
        public async Task ActivityIsAvailableDuringUnhandledExceptionCall()
        {
            var diagnosticSource = new DiagnosticListener("DummySource");
            var middleware = CreateDiagnosticMiddleware(diagnosticSource: diagnosticSource);
            var context = new DefaultHttpContext();

            bool endCalled = false;
            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair =>
            {
                if (pair.Key == "Microsoft.AspNetCore.Hosting.UnhandledException")
                {
                    endCalled = true;
                    Assert.NotNull(Activity.Current);
                    Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
                }
            }));

            var app = middleware(httpContext =>
            {
                throw new Exception();
            });

            await Assert.ThrowsAsync<Exception>(() => app(context));

            Assert.True(endCalled);
        }

        [Fact]
        public async Task ActivityIsAvailibleDuringRequest()
        {
            var diagnosticSource = new DiagnosticListener("DummySource");
            var middleware = CreateDiagnosticMiddleware(diagnosticSource: diagnosticSource);
            var context = new DefaultHttpContext();

            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair => { }),
                s =>
                {
                    if (s.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn"))
                    {
                        return true;
                    }
                    return false;
                });

            var app = middleware(httpContext =>
            {
                Assert.NotNull(Activity.Current);
                Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
                return Task.CompletedTask;
            });

            await app(context);
        }

        [Fact]
        public async Task ActivityParentIdAndBaggeReadFromHeaders()
        {
            var diagnosticSource = new DiagnosticListener("DummySource");
            var middleware = CreateDiagnosticMiddleware(diagnosticSource: diagnosticSource);
            var context = new DefaultHttpContext();
            context.Request.Headers["Request-Id"] = "ParentId1";
            context.Request.Headers["Correlation-Context"] = "Key1=value1, Key2=value2";

            diagnosticSource.Subscribe(new CallbackDiagnosticListener(pair => { }),
                s =>
                {
                    if (s.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn"))
                    {
                        return true;
                    }
                    return false;
                });

            var app = middleware(httpContext =>
            {
                Assert.Equal("Microsoft.AspNetCore.Hosting.HttpRequestIn", Activity.Current.OperationName);
                Assert.Equal("ParentId1", Activity.Current.ParentId);
                Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key1" && pair.Value == "value1");
                Assert.Contains(Activity.Current.Baggage, pair => pair.Key == "Key2" && pair.Value == "value2");

                return Task.CompletedTask;
            });

            await app(context);
        }

        private static void AssertProperty<T>(object o, string name)
        {
            Assert.NotNull(o);
            var property = o.GetType().GetTypeInfo().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property);
            var value = property.GetValue(o);
            Assert.NotNull(value);
            Assert.IsAssignableFrom<T>(value);
        }

        private static RequestDelegate CreateApplication(DiagnosticListener diagnosticSource = null, ILogger logger = null)
        {
            return CreateDiagnosticMiddleware(diagnosticSource, logger)(context => Task.CompletedTask);
        }

        private static Func<RequestDelegate, RequestDelegate> CreateDiagnosticMiddleware(DiagnosticListener diagnosticSource = null, ILogger logger = null)
        {
            return next => new RequestDiagnosticsMiddleware(next, diagnosticSource ?? new NoopDiagnosticSource(), logger ?? new NullScopeLogger()).Invoke;
        }

        private class NullScopeLogger : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
            }
        }

        private class LoggerWithScopes : ILogger
        {
            public IDisposable BeginScope<TState>(TState state)
            {
                Scopes.Add(state);
                return new Scope();
            }

            public List<object> Scopes { get; set; } = new List<object>();

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {

            }

            private class Scope : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }

        private class NoopDiagnosticSource : DiagnosticListener
        {
            public NoopDiagnosticSource() : base("DummyListener")
            {
            }

            public override bool IsEnabled(string name) => true;

            public override void Write(string name, object value)
            {
            }
        }

        private class CallbackDiagnosticListener : IObserver<KeyValuePair<string, object>>
        {
            private readonly Action<KeyValuePair<string, object>> _callback;

            public CallbackDiagnosticListener(Action<KeyValuePair<string, object>> callback)
            {
                _callback = callback;
            }

            public void OnNext(KeyValuePair<string, object> value)
            {
                _callback(value);
            }

            public void OnError(Exception error)
            {
            }

            public void OnCompleted()
            {
            }
        }
    }
}
