using System;
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
        public static readonly string OutputDirectoryEnvironmentVariableName = "ASPNETCORE_TEST_LOG_DIR";
        public static readonly string TestOutputRoot = Environment.GetEnvironmentVariable(OutputDirectoryEnvironmentVariableName);

        public static IDisposable Start<TTestClass>(ITestOutputHelper output, out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null)
        {
            loggerFactory = CreateLoggerFactory<TTestClass>(output, testName);
            var logger = loggerFactory.CreateLogger("TestLifetime");
            logger.LogInformation("Starting test {testName}", testName);
            return new Disposable(() => logger.LogInformation("Finished test {testName}", testName));
        }

        public static ILoggerFactory CreateLoggerFactory<TTestClass>(ITestOutputHelper output, [CallerMemberName] string testName = null)
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddXunit(output, LogLevel.Debug);

            if (!string.IsNullOrEmpty(TestOutputRoot))
            {
                var testClass = typeof(TTestClass).GetTypeInfo();
                var testOutputDir = Path.Combine(TestOutputRoot, testClass.Assembly.GetName().Name, testClass.FullName);
                if (!Directory.Exists(testOutputDir))
                {
                    Directory.CreateDirectory(testOutputDir);
                }

                var testOutputFile = Path.Combine(testOutputDir, $"{testName}.log");

                if (File.Exists(testOutputFile))
                {
                    File.Delete(testOutputFile);
                }

                var serilogger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .MinimumLevel.Verbose()
                    .WriteTo.File(testOutputFile, flushToDiskInterval: TimeSpan.FromSeconds(1))
                    .CreateLogger();
                loggerFactory.AddSerilog(serilogger);
            }

            return loggerFactory;
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
}
