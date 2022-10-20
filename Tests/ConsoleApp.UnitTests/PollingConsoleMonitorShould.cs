using Peckr.Abstractions;
using Peckr.Tests.Core.Generators;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Peckr.ConsoleApp.UnitTests
{
    [Collection("UnitTestFixtures")]
    public class PollingConsoleMonitorShould
    {
        private static readonly CancellationTokenSource NoCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
        
        private readonly Mock<IPeckResultPoller<int>> _mockPoller;
        private readonly Mock<IPeckResultSink<int>> _mockSink;
        private readonly PollingConsolePeckr<int> _pollingService;

        public PollingConsoleMonitorShould()
        {
            _mockPoller = new Mock<IPeckResultPoller<int>>();
            _mockSink = new Mock<IPeckResultSink<int>>();
            _pollingService = new PollingConsolePeckr<int>(_mockPoller.Object, _mockSink.Object);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        public async Task Complete_Successfully_When_Poller_Finishes(int count)
        {
            var settings = MonitorSettingsGenerator.Create(terminateWhenConditionMet: false);
            _mockPoller.Setup(m => m.PollAsync(It.IsAny<PeckrSettings>(), It.IsAny<CancellationToken>()))
                .Returns(GenerateAsyncEnumerable(count));

            var result = await _pollingService.PeckAsync(settings, NoCancellationTokenSource);

            result.Should().Be(ConsoleExitCode.Success);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        public async Task Complete_Successfully_When_The_Poller_Sees_A_Success_And_TerminateWhenConditionMet_Is_True(int count)
        {
            var settings = MonitorSettingsGenerator.Create(terminateWhenConditionMet: true);
            _mockPoller.Setup(m => m.PollAsync(It.IsAny<PeckrSettings>(), It.IsAny<CancellationToken>()))
                .Returns(GenerateAsyncEnumerable(count));

            var result = await _pollingService.PeckAsync(settings, NoCancellationTokenSource);

            result.Should().Be(ConsoleExitCode.Success);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public async Task Returns_UnknownError_When_The_Poller_Sees_A_Failure_And_TerminateWhenConditionMet_Is_True(int count)
        {
            var settings = MonitorSettingsGenerator.Create(terminateWhenConditionMet: true);
            _mockPoller.Setup(m => m.PollAsync(It.IsAny<PeckrSettings>(), It.IsAny<CancellationToken>()))
                .Returns(GenerateAsyncEnumerable(count, i => PeckResult<int>.Failure(i)));

            var result = await _pollingService.PeckAsync(settings, NoCancellationTokenSource);

            result.Should().Be(ConsoleExitCode.UnknownError);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public async Task Returns_TimedOut_When_The_Poller_Sees_A_Failure_And_ShouldFailOnRunDurationExceeded_Is_True(int count)
        {
            var settings = MonitorSettingsGenerator.Create(shouldFailOnRunDurationExceeded: true);
            _mockPoller.Setup(m => m.PollAsync(It.IsAny<PeckrSettings>(), It.IsAny<CancellationToken>()))
                .Returns(GenerateAsyncEnumerable(count, i => PeckResult<int>.TimedOut(i)));

            var result = await _pollingService.PeckAsync(settings, NoCancellationTokenSource);

            result.Should().Be(ConsoleExitCode.TimedOut);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        public async Task Try_To_Push_Results_To_The_Sink(int count)
        {
            var settings = MonitorSettingsGenerator.Create(terminateWhenConditionMet: false);
            _mockPoller.Setup(m => m.PollAsync(It.IsAny<PeckrSettings>(), It.IsAny<CancellationToken>()))
                .Returns(GenerateAsyncEnumerable(count));

            var result = await _pollingService.PeckAsync(settings, NoCancellationTokenSource);

            _mockSink.Verify(
                s => s.PushMonitoringResultAsync(It.IsAny<PeckResult<int>>(), It.IsAny<PeckrSettings>(), It.IsAny<CancellationToken>()), 
                Times.Exactly(count));

        }

        private static async IAsyncEnumerable<PeckResult<int>> GenerateAsyncEnumerable(
            int count, Func<int, PeckResult<int>> seedToResultGenerator = null, int delayMilliseconds = 1)
        {
            for (var i = 0; i < count; i++)
            {
                yield return seedToResultGenerator == null 
                    ? PeckResult<int>.Polling(i) 
                    : seedToResultGenerator(i);

                await Task.Delay(delayMilliseconds).ConfigureAwait(false);
            }
        }
    }
}
