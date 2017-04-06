// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.xunit
{
    public static class TestLogging
    {
        // Need to qualify because of Serilog.ILogger :(
        private static readonly Extensions.Logging.ILogger GlobalLogger;

        public static readonly string OutputDirectoryEnvironmentVariableName = "ASPNETCORE_TEST_LOG_DIR";
        public static readonly string TestOutputRoot;

        static TestLogging()
        {
            TestOutputRoot = Environment.GetEnvironmentVariable(OutputDirectoryEnvironmentVariableName);
            GlobalLogger = CreateGlobalLogger(TestOutputRoot);
        }

        public static IDisposable Start<TTestClass>(ITestOutputHelper output, out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null) =>
            Start(output, out loggerFactory, typeof(TTestClass).GetTypeInfo().Assembly.GetName().Name, typeof(TTestClass).FullName, testName);

        public static IDisposable Start(ITestOutputHelper output, out ILoggerFactory loggerFactory, string assemblyName, string className, [CallerMemberName] string testName = null)
        {
            loggerFactory = CreateLoggerFactory(output, assemblyName, className, testName);
            var logger = loggerFactory.CreateLogger("TestLifetime");

            var stopwatch = Stopwatch.StartNew();
            GlobalLogger.LogInformation("Starting test {testName}", testName);
            logger.LogInformation("Starting test {testName}", testName);
            return new Disposable(() =>
            {
                stopwatch.Stop();
                GlobalLogger.LogInformation("Finished test {testName} in {duration}s", testName, stopwatch.Elapsed.TotalSeconds);
                logger.LogInformation("Finished test {testName} in {duration}s", testName, stopwatch.Elapsed.TotalSeconds);
            });
        }

        public static ILoggerFactory CreateLoggerFactory<TTestClass>(ITestOutputHelper output, [CallerMemberName] string testName = null) =>
            CreateLoggerFactory(output, typeof(TTestClass).GetTypeInfo().Assembly.GetName().Name, typeof(TTestClass).FullName, testName);

        public static ILoggerFactory CreateLoggerFactory(ITestOutputHelper output, string assemblyName, string className, [CallerMemberName] string testName = null)
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddXunit(output, LogLevel.Debug);

            // Try to shorten the class name using the assembly name
            if (className.StartsWith(assemblyName + "."))
            {
                className = className.Substring(assemblyName.Length + 1);
            }

            var testOutputFile = Path.Combine(assemblyName, className, $"{testName}.log");
            AddFileLogging(loggerFactory, testOutputFile);

            return loggerFactory;
        }

        // Need to qualify because of Serilog.ILogger :(
        private static Extensions.Logging.ILogger CreateGlobalLogger(string testOutputRoot)
        {
            var loggerFactory = new LoggerFactory();

            // Let the global logger log to the console, it's just "Starting X..." "Finished X..."
            loggerFactory.AddConsole();

            // We can't use entry assembly, because it's testhost
            // We can't use process mainmodule, because it's dotnet.exe
            // So we're left with this...
            string appName;
            var files = Directory.GetFiles(AppContext.BaseDirectory, "*.runtimeconfig.json");
            if (files.Length == 0)
            {
                // Just use a GUID...
                appName = Guid.NewGuid().ToString("N");
            }
            else
            {
                // Strip off .json and .runtimeconfig
                appName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(files[0]));
            }

            var globalLogFileName = Path.Combine(appName, "global.log");
            AddFileLogging(loggerFactory, globalLogFileName);

            var logger = loggerFactory.CreateLogger("GlobalTestLog");
            logger.LogInformation($"Global Test Logging initialized. Set the '{OutputDirectoryEnvironmentVariableName}' Environment Variable to a path in order to save logs to files");
            return logger;
        }

        private static void AddFileLogging(ILoggerFactory loggerFactory, string fileName)
        {
            if (!string.IsNullOrEmpty(TestOutputRoot))
            {
                fileName = Path.Combine(TestOutputRoot, fileName);

                var dir = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                var serilogger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .MinimumLevel.Verbose()
                    .WriteTo.File(fileName, flushToDiskInterval: TimeSpan.FromSeconds(1))
                    .CreateLogger();
                loggerFactory.AddSerilog(serilogger);
            }
        }

        private class Disposable : IDisposable
        {
            private Action _action;

            public Disposable(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }
    }

    public abstract class LoggedTest
    {
        private readonly ITestOutputHelper _output;

        protected LoggedTest(ITestOutputHelper output)
        {
            _output = output;
        }

        public IDisposable StartLog(out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null)
        {
            return TestLogging.Start(
                _output,
                out loggerFactory,
                GetType().GetTypeInfo().Assembly.GetName().Name,
                GetType().FullName,
                testName);
        }
    }
}
