using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Peckr.Abstractions;
using Peckr.Metrics.UnitTests.Generators;
using Peckr.Tests.Core.Generators;
using FluentAssertions;
using Moq;
using Xunit;

namespace Peckr.Metrics.UnitTests
{
    [Collection("UnitTestFixtures")]
    public class InstanceMetricAverageUpperThresholdPollerShould
    {
        private readonly Mock<IMonitorDataRetriever<IReadOnlyCollection<Metric>>> _mockRetriever;
        private readonly InstanceMetricAverageUpperThresholdPoller _poller;

        public InstanceMetricAverageUpperThresholdPollerShould()
        {
            _mockRetriever = new Mock<IMonitorDataRetriever<IReadOnlyCollection<Metric>>>();
            _poller = new InstanceMetricAverageUpperThresholdPoller(_mockRetriever.Object);
        }

        [Theory]
        [InlineData(1, 0, 140, 160, 100)]
        [InlineData(3, 38, 22, 48, 36)]
        public async Task Derive_Correct_Average_Metric_Value(
            int runDurationSeconds,
            int firstMetricValue,
            int secondMetricValue,
            int thirdMetricValue,
            int expectedMetricValue)
        {
            var settings = MonitorSettingsGenerator.Create(
                expectedRunDurationSeconds: runDurationSeconds,
                pollingDelayMilliseconds: 200,
                primaryThresholdValue: 100);
            _mockRetriever
                .Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    MetricGenerator.OfCount(
                        3,
                        i => i switch
                        {
                            0 => firstMetricValue,
                            1 => secondMetricValue,
                            2 => thirdMetricValue,
                            _ => 0
                        }));

            await foreach (var monitorResult in _poller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
            {
                monitorResult.Outcome.Should().Be(MonitoringOutcome.Polling);
                monitorResult.Result.Count.Should().Be(1); // reduced to 1 average value regardless of the actual metric count
                monitorResult.Result.First().Value.Should().Be(expectedMetricValue);
            }
        }

        [Theory]
        [InlineData(1, 350, 80, 24, 3)]
        [InlineData(5, 1050, 75, 75, 5)]
        public async Task Keep_Polling_For_Expected_Duration_Using_Polling_Interval_When_Threshold_Condition_Is_Not_Met(
            int runDurationSeconds,
            int pollingDelayMilliseconds,
            int thresholdSetting,
            int actualMetricValue,
            int expectedPollCount)
        {
            var settings = MonitorSettingsGenerator.Create(
                expectedRunDurationSeconds: runDurationSeconds,
                pollingDelayMilliseconds: pollingDelayMilliseconds,
                primaryThresholdValue: thresholdSetting);
            _mockRetriever
                .Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MetricGenerator.OfCount(1, i => actualMetricValue));

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
        [InlineData(2, 525, 80, 81, 1, 4)]
        [InlineData(6, 1050, 75, 95, 10, 6)]
        public async Task Keep_Polling_And_Return_Failures_When_Metrics_Exceed_Threshold_Given_TerminateWhenConditionMet_Is_False(
            int runDurationSeconds,
            int pollingDelayMilliseconds,
            int thresholdSetting,
            int metricThresholdValue,
            int metricCount,
            int expectedPollCount)
        {
            var metrics = MetricGenerator.OfCount(metricCount, i => metricThresholdValue);
            var settings = MonitorSettingsGenerator.Create(
                expectedRunDurationSeconds: runDurationSeconds,
                pollingDelayMilliseconds: pollingDelayMilliseconds,
                primaryThresholdValue: thresholdSetting,
                cooldownWhenConditionMetMilliseconds: 0,
                terminateWhenConditionMet:false);
            _mockRetriever
                .Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(metrics);

            var pollCount = 0;
            await foreach (var result in _poller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
            {
                result.Outcome.Should().Be(MonitoringOutcome.Failure);
                result.Result.Count.Should().Be(1); // reduced to 1 average value regardless of the actual metric count
                pollCount++;
            }

            pollCount.Should().BeGreaterOrEqualTo(expectedPollCount);
            pollCount.Should().BeLessOrEqualTo(expectedPollCount + 1);
        }

        [Fact]
        public async Task Terminate_Immediately_With_Failure_When_Metrics_Exceed_Threshold_Given_TerminateWhenConditionMet_Is_True()
        {
            var metrics = MetricGenerator.OfCount(1, i => 100);
            var settings = MonitorSettingsGenerator.Create(
                expectedRunDurationSeconds: 300,
                pollingDelayMilliseconds: 2000,
                primaryThresholdValue: 80,
                cooldownWhenConditionMetMilliseconds: 0,
                terminateWhenConditionMet: true);
            _mockRetriever
                .Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(metrics);

            var pollCount = 0;
            await foreach (var result in _poller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
            {
                result.Outcome.Should().Be(MonitoringOutcome.Failure);
                result.Result.Count.Should().Be(1);
                pollCount++;
            }

            pollCount.Should().Be(1);
        }

        [Fact]
        public async Task Keep_Polling_Until_Metrics_Exceed_Threshold_Given_TerminateWhenConditionMet_Is_True()
        {
            var settings = MonitorSettingsGenerator.Create(
                expectedRunDurationSeconds: 60,
                pollingDelayMilliseconds: 200,
                primaryThresholdValue: 25,
                cooldownWhenConditionMetMilliseconds: 0,
                terminateWhenConditionMet: true);
            _mockRetriever
                .Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new Queue<Metric[]>(
                        new[] 
                        {
                            MetricGenerator.OfCount(3, i => 20),
                            MetricGenerator.OfCount(3, i => 25),
                            MetricGenerator.OfCount(3, i => 30), // 3rd retrieval is a failure
                            MetricGenerator.OfCount(3, i => 35)
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
        public async Task Ensure_Settings_Are_Carried_Through_To_Retriever_When_Querying_Metrics(
            int previousSpanMinutes, int takeLimit, string filter)
        {
            var settings = MonitorSettingsGenerator.Create(
                expectedRunDurationSeconds: 1,
                pollingDelayMilliseconds: 500,
                sourcePreviousSpanSeconds: previousSpanMinutes * 60,
                sourceTakeLimit: takeLimit,
                sourceFilter: filter,
                primaryThresholdValue: 75);
            _mockRetriever
                .Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { MetricGenerator.Create(valueProvider: i => 70) });

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
        [InlineData(2, 200, 2000, 1)]
        [InlineData(4, 500, 500, 4)]
        public async Task Cooldown_For_Expected_Duration_When_Metrics_Exceed_Threshold_Given_TerminateWhenConditionMet_Is_False(
            int runDurationSeconds,
            int pollingDelayMilliseconds,
            int cooldownWhenConditionMetMilliseconds,
            int expectedPollCount)
        {
            var metrics = MetricGenerator.OfCount(5, i => 85);
            var settings = MonitorSettingsGenerator.Create(
                expectedRunDurationSeconds: runDurationSeconds,
                pollingDelayMilliseconds: pollingDelayMilliseconds,
                primaryThresholdValue: 75,
                cooldownWhenConditionMetMilliseconds: cooldownWhenConditionMetMilliseconds,
                terminateWhenConditionMet: false);
            _mockRetriever
                .Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(metrics);

            var pollCount = 0;
            var sw = Stopwatch.StartNew();
            await foreach (var result in _poller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
            {
                result.Outcome.Should().Be(MonitoringOutcome.Failure);
                result.Result.Count.Should().Be(1); // reduced to 1 average value regardless of the actual metric count
                pollCount++;
            }

            const int durationErrorMarginMilliseconds = 300;
            pollCount.Should().Be(expectedPollCount);
            sw.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(runDurationSeconds * 1000);
            sw.ElapsedMilliseconds.Should().BeLessOrEqualTo(runDurationSeconds * 1000 + durationErrorMarginMilliseconds);
        }
    }
}