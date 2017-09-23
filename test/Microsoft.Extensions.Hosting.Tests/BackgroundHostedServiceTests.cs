using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class BackgroundHostedServiceTests
    {
        [Fact]
        public async Task StopAsyncThrowsIfBackgroundTaskThrows()
        {
            var service = new SynchronousExceptionService();

            await service.StartAsync(CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.StopAsync(CancellationToken.None));
        }

        [Fact]
        public async Task StopAsyncThrowsIfExecuteAsyncFails()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.TrySetException(new Exception("fail!"));
            var service = new MyBackgroundService(tcs.Task);

            await service.StartAsync(CancellationToken.None);

            var exception = await Assert.ThrowsAsync<Exception>(() => service.StopAsync(CancellationToken.None));

            Assert.Equal("fail!", exception.Message);
        }

        [Fact]
        public async Task StopAsyncWithoutStartAsyncNoops()
        {
            var tcs = new TaskCompletionSource<object>();
            var service = new MyBackgroundService(tcs.Task);

            await service.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task StopAsyncStopsBackgroundService()
        {
            var tcs = new TaskCompletionSource<object>();
            var service = new MyBackgroundService(tcs.Task);

            await service.StartAsync(CancellationToken.None);

            Assert.False(service.ExecutingTask.IsCompleted);

            await service.StopAsync(CancellationToken.None);

            Assert.True(service.ExecutingTask.IsCompleted);
        }

        [Fact]
        public async Task StopAsyncStopsEvenIfTaskNeverEnds()
        {
            var service = new IgnoreCancellationService();

            await service.StartAsync(CancellationToken.None);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await service.StopAsync(cts.Token);
        }

        [Fact]
        public async Task StartAsyncThenDisposeTriggersCancelledToken()
        {
            var service = new WaitForCancelledTokenService();

            await service.StartAsync(CancellationToken.None);

            service.Dispose();

            await Assert.ThrowsAsync<TaskCanceledException>(() => service.ExecutingTask);
        }

        [Fact]
        public async Task BlockingServiceCanBeStopped()
        {
            using (var service = new BlockingService())
            {
                await service.StartAsync(CancellationToken.None);

                await service.StopAsync(CancellationToken.None);

                service.Dispose();
            }
        }

        private class SynchronousExceptionService : BackgroundService
        {
            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                throw new InvalidOperationException();
            }
        }

        private class WaitForCancelledTokenService : BackgroundService
        {
            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                return Task.Delay(Timeout.Infinite, stoppingToken);
            }
        }

        private class ThrowOnCancellationService : BackgroundService
        {
            public int TokenCalls { get; set; }

            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                stoppingToken.Register(() =>
                {
                    TokenCalls++;
                    throw new InvalidOperationException();
                });

                stoppingToken.Register(() =>
                {
                    TokenCalls++;
                });

                return new TaskCompletionSource<object>().Task;
            }
        }

        private class BlockingService : BackgroundService
        {
            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                stoppingToken.WaitHandle.WaitOne();

                return Task.CompletedTask;
            }
        }

        private class IgnoreCancellationService : BackgroundService
        {
            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                return new TaskCompletionSource<object>().Task;
            }
        }

        private class MyBackgroundService : BackgroundService
        {
            private readonly Task _task;

            public MyBackgroundService(Task task)
            {
                _task = task;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                var task = await Task.WhenAny(_task, Task.Delay(Timeout.Infinite, stoppingToken));

                await task;
            }
        }
    }
}
