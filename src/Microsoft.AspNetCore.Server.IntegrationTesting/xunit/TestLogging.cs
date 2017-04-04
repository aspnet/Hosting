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
        // Need to qualify because of Serilog.ILogger :(
        private static readonly Extensions.Logging.ILogger GlobalLogger = CreateGlobalLogger();

        public static readonly string OutputDirectoryEnvironmentVariableName = "ASPNETCORE_TEST_LOG_DIR";
        public static readonly string TestOutputRoot = Environment.GetEnvironmentVariable(OutputDirectoryEnvironmentVariableName);

        public static IDisposable Start<TTestClass>(ITestOutputHelper output, out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null)
        {
            loggerFactory = CreateLoggerFactory<TTestClass>(output, testName);
            var logger = loggerFactory.CreateLogger("TestLifetime");

            GlobalLogger.LogInformation("Starting test {testName}", testName);
            logger.LogInformation("Starting test {testName}", testName);
            return new Disposable(() =>
            {
                GlobalLogger.LogInformation("Finished test {testName}", testName);
                logger.LogInformation("Finished test {testName}", testName);
            });
        }

        public static ILoggerFactory CreateLoggerFactory<TTestClass>(ITestOutputHelper output, [CallerMemberName] string testName = null)
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddXunit(output, LogLevel.Debug);

            var testClass = typeof(TTestClass).GetTypeInfo();
            var asmName = testClass.Assembly.GetName().Name;
            var className = testClass.FullName;

            // Try to shorten the class name using the assembly name
            if (className.StartsWith(asmName + "."))
            {
                className = className.Substring(asmName.Length + 1);
            }

            var testOutputFile = Path.Combine(asmName, className, $"{testName}.log");
            AddFileLogging(loggerFactory, testOutputFile);

            return loggerFactory;
        }

        // Need to qualify because of Serilog.ILogger :(
        private static Extensions.Logging.ILogger CreateGlobalLogger()
        {
            var loggerFactory = new LoggerFactory();

#if NET46
            var appName = Assembly.GetEntryAssembly().GetName().Name;
#else
            // GROOOSSS but should work...
            string appName;
            var files = Directory.GetFiles(AppContext.BaseDirectory, "*.runtimeconfig.json");
            if(files.Length == 0)
            {
                // Even Grosser
                appName = Guid.NewGuid().ToString("N");
            }
            else
            {
                appName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(files[0]));
            }
#endif

            AddFileLogging(loggerFactory, Path.Combine(appName, "global.log"));

            return loggerFactory.CreateLogger("GlobalTestLog");
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
}
