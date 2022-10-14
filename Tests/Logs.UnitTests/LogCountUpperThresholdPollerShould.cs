using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticsMonitor.Abstractions;
using DiagnosticsMonitor.Logs.UnitTests.Generators;
using DiagnosticsMonitor.Tests.Core.Generators;
using FluentAssertions;
using Moq;
using Xunit;

namespace DiagnosticsMonitor.Logs.UnitTests
{
    [Collection("UnitTestFixtures")]
    public class LogCountUpperThresholdPollerShould
    {
        private readonly Mock<IMonitorDataRetriever<IReadOnlyCollection<LogEntry>>> _mockRetriever;
        private readonly LogCountUpperThresholdPoller _poller;

        public LogCountUpperThresholdPollerShould()
        {
            _mockRetriever = new Mock<IMonitorDataRetriever<IReadOnlyCollection<LogEntry>>>();
            _poller = new LogCountUpperThresholdPoller(_mockRetriever.Object);
        }

        [Theory]
        [InlineData(1, 350, 0, 0, 3)]
        [InlineData(5, 1050, 10, 9, 5)]
        public async Task Keep_Polling_For_Expected_Duration_Using_Polling_Interval_When_Log_Count_Does_Not_Exceed_Threshold(
            int runDurationSeconds,
            int pollingDelayMilliseconds,
            int countThresholdSetting,
            int actualCount,
            int expectedPollCount)
        {
            _mockRetriever
                .Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(LogEntryGenerator.OfCount(actualCount));
            var settings = MonitorSettingsGenerator.Create(
                expectedRunDurationSeconds: runDurationSeconds,
                pollingDelayMilliseconds: pollingDelayMilliseconds,
                primaryThresholdValue: countThresholdSetting);
            
            var pollCount = 0;
            var sw = Stopwatch.StartNew();
            await foreach (var monitorResult in _poller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
            {
                monitorResult.Outcome.Should().Be(MonitoringOutcome.Polling);
                pollCount++;
            }
            sw.Stop();

            const int durationErrorMarginMilliseconds = 400;
            pollCount.Should().Be(expectedPollCount);
            sw.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(runDurationSeconds * 1000);
            sw.ElapsedMilliseconds.Should().BeLessOrEqualTo(runDurationSeconds * 1000 + durationErrorMarginMilliseconds);
        }

        [Theory]
        [InlineData(2, 525, 0, 1, 4)]
        [InlineData(6, 1050, 10, 80, 6)]
        public async Task Keep_Polling_And_Return_Failures_When_Log_Count_Exceeds_Threshold_Given_TerminateWhenConditionMet_Is_False(
            int runDurationSeconds, 
            int pollingDelayMilliseconds, 
            int countThresholdSetting, 
            int actualCount,
            int expectedPollCount)
        {
            var logEntries = LogEntryGenerator.OfCount(actualCount);
            var settings = MonitorSettingsGenerator.Create(
                expectedRunDurationSeconds: runDurationSeconds,
                pollingDelayMilliseconds: pollingDelayMilliseconds,
                primaryThresholdValue: countThresholdSetting,
                cooldownWhenConditionMetMilliseconds: 0,
                terminateWhenConditionMet: false);
            _mockRetriever
                .Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(logEntries);

            var pollCount = 0;
            await foreach (var result in _poller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
            {
                result.Outcome.Should().Be(MonitoringOutcome.Failure);
                result.Result.Count.Should().Be(logEntries.Length);
                for (var i = 0; i < result.Result.Count; i++)
                {
                    result.Result.ElementAt(i).Should().Be(logEntries[i]);
                }
                pollCount++;
            }

            pollCount.Should().Be(expectedPollCount);
        }

        [Fact]
        public async Task Terminate_Immediately_With_Failure_When_Log_Count_Exceeds_Threshold_Given_TerminateWhenConditionMet_Is_True()
        {
            var logEntries = LogEntryGenerator.OfCount(100);
            var settings = MonitorSettingsGenerator.Create(
                expectedRunDurationSeconds: 300,
                pollingDelayMilliseconds: 2000,
                primaryThresholdValue: 0,
                terminateWhenConditionMet: true);
            _mockRetriever
                .Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(logEntries);

            var pollCount = 0;
            await foreach (var result in _poller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
            {
                result.Outcome.Should().Be(MonitoringOutcome.Failure);
                result.Result.Count.Should().Be(100);
                for (var i = 0; i < result.Result.Count; i++)
                {
                    result.Result.ElementAt(i).Should().Be(logEntries[i]);
                }
                pollCount++;
            }

            pollCount.Should().Be(1);
        }

        [Fact]
        public async Task Keep_Polling_Until_Log_Count_Exceeds_Threshold_Given_TerminateWhenConditionMet_Is_True()
        {
            var settings = MonitorSettingsGenerator.Create(
                expectedRunDurationSeconds: 60,
                pollingDelayMilliseconds: 200,
                primaryThresholdValue: 0,
                terminateWhenConditionMet: true);
            _mockRetriever
                .Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new Queue<LogEntry[]>(
                        new[]
                        {
                            LogEntryGenerator.OfCount(0),
                            LogEntryGenerator.OfCount(0),
                            LogEntryGenerator.OfCount(1), // 3rd retrieval is a failure
                            LogEntryGenerator.OfCount(0)
                        }).Dequeue);

            var pollCount = 0;
            await foreach (var result in _poller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
            {
                pollCount++;
                result.Outcome.Should().Be(pollCount == 3 ? MonitoringOutcome.Failure : MonitoringOutcome.Polling);
            }

            pollCount.Should().Be(3);
        }

        [Theory]
        [InlineData(5, 10, "")]
        [InlineData(1, 5, "EventId ne 100")]
        [InlineData(10, 30, "1=1")]
        public async Task Ensure_Settings_Are_Carried_Through_To_Retriever_When_Querying_Logs(
            int previousSpanMinutes, int takeLimit, string filter)
        {
            _mockRetriever
                .Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<LogEntry>());
            var settings = MonitorSettingsGenerator.Create(
                expectedRunDurationSeconds: 1,
                pollingDelayMilliseconds: 500,
                sourcePreviousSpanSeconds: previousSpanMinutes * 60,
                sourceTakeLimit: takeLimit,
                sourceFilter: filter);

            var pollCount = 0;
            await foreach (var _ in _poller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
            {
                pollCount++;
            }

            _mockRetriever.Verify(
                r => r.GetAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), takeLimit, filter, true, CancellationToken.None),
                Times.Exactly(pollCount));
        }

        [Theory]
        [InlineData(2, 220, 2000, 1)]
        [InlineData(4, 550, 550, 4)]
        public async Task Cooldown_For_Expected_Duration_When_Log_Count_Exceeds_Threshold_Given_TerminateWhenConditionMet_Is_False(
            int runDurationSeconds,
            int pollingDelayMilliseconds,
            int cooldownWhenConditionMetMilliseconds,
            int expectedPollCount)
        {
            var logEntries = LogEntryGenerator.OfCount(10);
            var settings = MonitorSettingsGenerator.Create(
                expectedRunDurationSeconds: runDurationSeconds,
                pollingDelayMilliseconds: pollingDelayMilliseconds,
                primaryThresholdValue: 0,
                cooldownWhenConditionMetMilliseconds: cooldownWhenConditionMetMilliseconds,
                terminateWhenConditionMet: false);
            _mockRetriever
                .Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(logEntries);

            var pollCount = 0;
            var sw = Stopwatch.StartNew();
            await foreach (var result in _poller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
            {
                result.Outcome.Should().Be(MonitoringOutcome.Failure);
                pollCount++;
            }

            const int durationErrorMarginMilliseconds = 550;
            pollCount.Should().Be(expectedPollCount);
            sw.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(runDurationSeconds * 1000);
            sw.ElapsedMilliseconds.Should().BeLessOrEqualTo(runDurationSeconds * 1000 + durationErrorMarginMilliseconds);
        }
    }
}