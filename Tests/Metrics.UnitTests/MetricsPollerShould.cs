using Peckr.Abstractions;
using Peckr.Metrics.UnitTests.Generators;
using Peckr.Tests.Core.Generators;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Peckr.Tests.Core.Extensions;
using Xunit;
using Range = Moq.Range;

namespace Peckr.Metrics.UnitTests
{
    public class MetricsPollerShould
    {
        private readonly MetricsPoller _successPoller;
        private readonly MetricsPoller _failurePoller;
        private readonly Mock<IPeckDataRetriever<IReadOnlyCollection<Metric>>> _mockRetriever;
        private readonly Mock<IPeckResultEvaluator<IReadOnlyCollection<Metric>>> _mockEvaluator;

        public MetricsPollerShould()
        {
            _mockRetriever = new Mock<IPeckDataRetriever<IReadOnlyCollection<Metric>>>();
            _mockEvaluator = new Mock<IPeckResultEvaluator<IReadOnlyCollection<Metric>>>();
            _successPoller = new MetricsPoller(_mockRetriever.Object, _mockEvaluator.Object);
            _failurePoller = new MetricsPoller(_mockRetriever.Object, _mockEvaluator.Object, false);
        }

        [Fact]
        public async Task Return_Success_When_Evaluator_Returns_IsConditionMet_True()
        {
            const int valueWithinSecondaryThreshold = 90;
            const int primaryThresholdValue = 70;
            const int secondaryThresholdValue = 90;

            var settings = MonitorSettingsGenerator.Create(
                10,
                30,
                primaryThresholdValue: primaryThresholdValue,
                secondaryThresholdValue: secondaryThresholdValue);

            _mockEvaluator.Setup(e => e.IsConditionMet(It.IsAny<IReadOnlyCollection<Metric>>())).Returns(true);

            _mockRetriever.Setup(x =>
                    x.GetAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MetricGenerator.OfPercentage(valueWithinSecondaryThreshold, s => primaryThresholdValue, f => primaryThresholdValue + 0.1));

            var pollCount = 0;
            await foreach (var peckrResult in _successPoller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
            {
                peckrResult.Outcome.Should().Be(MonitoringOutcome.Success);
                pollCount++;
            }

            pollCount.Should().Be(1);
        }

        [Fact]
        public async Task Return_Failure_When_Evaluator_Returns_IsConditionMet_True()
        {
            const int valueWithinSecondaryThreshold = 90;
            const int primaryThresholdValue = 70;
            const int secondaryThresholdValue = 90;

            var settings = MonitorSettingsGenerator.Create(
                10,
                30,
                primaryThresholdValue: primaryThresholdValue,
                secondaryThresholdValue: secondaryThresholdValue);

            _mockEvaluator.Setup(e => e.IsConditionMet(It.IsAny<IReadOnlyCollection<Metric>>())).Returns(true);

            _mockRetriever.Setup(x =>
                    x.GetAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MetricGenerator.OfPercentage(valueWithinSecondaryThreshold, s => primaryThresholdValue, f => primaryThresholdValue + 0.1));

            var pollCount = 0;
            await foreach (var peckrResult in _failurePoller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
            {
                peckrResult.Outcome.Should().Be(MonitoringOutcome.Failure);
                pollCount++;
            }

            pollCount.Should().Be(1);
        }

        [Fact]
        public async Task Eventually_Returns_Success_Evaluator_Returns_True()
        {
            const int valueWithinSecondaryThreshold = 81;
            const int primaryThresholdValue = 60;
            const int secondaryThresholdValue = 80;
            const int runtimeDuration = 10;
            const int pollingDelay = 30;

            var settings = MonitorSettingsGenerator.Create(
                runtimeDuration,
                pollingDelay,
                primaryThresholdValue: primaryThresholdValue,
                secondaryThresholdValue: secondaryThresholdValue);

            _mockEvaluator.SetupSequence(e => e.IsConditionMet(It.IsAny<IReadOnlyCollection<Metric>>()))
                    .Returns(false)
                    .Returns(false)
                    .Returns(false)
                    .Returns(true);

            _mockRetriever.Setup(x =>
                    x.GetAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MetricGenerator.OfPercentage(valueWithinSecondaryThreshold, s => primaryThresholdValue, f => primaryThresholdValue + 0.1));

            var expectedMonitoringOutcomes = new[]
            {
                MonitoringOutcome.Polling,
                MonitoringOutcome.Polling,
                MonitoringOutcome.Polling,
                MonitoringOutcome.Success
            };

            var pollCount = 0;
            await foreach (var peckrResult in _successPoller.PollAsync(settings, CancellationToken.None)
                .ConfigureAwait(false))
            {
                peckrResult.Outcome.Should().Be(expectedMonitoringOutcomes[pollCount]);
                pollCount++;
            }
            pollCount.Should().Be(4);
        }

        [Fact]
        public async Task Eventually_Returns_Failure_Evaluator_Returns_True()
        {
            const int valueWithinSecondaryThreshold = 81;
            const int primaryThresholdValue = 60;
            const int secondaryThresholdValue = 80;
            const int runtimeDuration = 10;
            const int pollingDelay = 30;

            var settings = MonitorSettingsGenerator.Create(
                runtimeDuration,
                pollingDelay,
                primaryThresholdValue: primaryThresholdValue,
                secondaryThresholdValue: secondaryThresholdValue);

            _mockEvaluator.SetupSequence(e => e.IsConditionMet(It.IsAny<IReadOnlyCollection<Metric>>()))
                .Returns(false)
                .Returns(false)
                .Returns(false)
                .Returns(true);

            _mockRetriever.Setup(x =>
                    x.GetAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MetricGenerator.OfPercentage(valueWithinSecondaryThreshold, s => primaryThresholdValue, f => primaryThresholdValue + 0.1));

            var expectedMonitoringOutcomes = new[]
            {
                MonitoringOutcome.Polling,
                MonitoringOutcome.Polling,
                MonitoringOutcome.Polling,
                MonitoringOutcome.Failure
            };

            var pollCount = 0;
            await foreach (var peckrResult in _failurePoller.PollAsync(settings, CancellationToken.None)
                .ConfigureAwait(false))
            {
                peckrResult.Outcome.Should().Be(expectedMonitoringOutcomes[pollCount]);
                pollCount++;
            }
            pollCount.Should().Be(4);
        }

        [Fact]
        public async Task Return_Polling_When_Evaluator_Returns_IsConditionMet_False_And_ShouldEmitSuccessWhenConditionIsMet_Is_False()
        {
            const int valueNotWithinSecondaryThreshold = 81;
            const int primaryThresholdValue = 80;
            const int secondaryThresholdValue = 82;
            const int runtimeDuration = 1;
            const int pollingDelay = 70;
            const double weightedExecutionTime = 0.07;

            var settings = MonitorSettingsGenerator.Create(
                runtimeDuration,
                pollingDelay,
                primaryThresholdValue: primaryThresholdValue,
                secondaryThresholdValue: secondaryThresholdValue);

            _mockEvaluator.Setup(e => e.IsConditionMet(It.IsAny<IReadOnlyCollection<Metric>>())).Returns(false);

            _mockRetriever.Setup(x =>
                    x.GetAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MetricGenerator.OfPercentage(valueNotWithinSecondaryThreshold, s => primaryThresholdValue, f => primaryThresholdValue + 0.1));

            var sw = await ActionTimer.MeasureAsync(async () =>
            {
                await foreach (var peckrResult in _failurePoller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
                {
                    peckrResult.Outcome.Should().Be(MonitoringOutcome.Polling);
                }
            });

            var runtimeDurationInMs = runtimeDuration * 1000;
            var invocationCount = _mockRetriever.Invocations.Select(i => i.Method.Name == "GetAsync").Count();
            var estimatedCodeExecutionTime = (int)Math.Ceiling(pollingDelay * weightedExecutionTime);
            var estimatedTotalCodeExecutionTime = estimatedCodeExecutionTime * invocationCount;

            _mockRetriever.Verify(x => x.GetAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Between(runtimeDurationInMs / (pollingDelay + estimatedCodeExecutionTime), runtimeDurationInMs / pollingDelay + 1, Range.Inclusive));

            sw.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(Math.Min(runtimeDuration, invocationCount) * 1000);
            sw.ElapsedMilliseconds.Should().BeLessOrEqualTo(Math.Max(runtimeDurationInMs + estimatedTotalCodeExecutionTime, invocationCount * pollingDelay + estimatedTotalCodeExecutionTime));
        }

        [Fact]
        public async Task Return_Polling_When_Evaluator_Returns_IsConditionMet_False_And_ShouldEmitSuccessWhenConditionIsMet_Is_True()
        {
            const int valueNotWithinSecondaryThreshold = 81;
            const int primaryThresholdValue = 80;
            const int secondaryThresholdValue = 82;
            const int runtimeDuration = 1;
            const int pollingDelay = 70;
            const double weightedExecutionTime = 0.07;

            var settings = MonitorSettingsGenerator.Create(
                runtimeDuration,
                pollingDelay,
                primaryThresholdValue: primaryThresholdValue,
                secondaryThresholdValue: secondaryThresholdValue);

            _mockEvaluator.Setup(e => e.IsConditionMet(It.IsAny<IReadOnlyCollection<Metric>>())).Returns(false);

            _mockRetriever.Setup(x =>
                    x.GetAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MetricGenerator.OfPercentage(valueNotWithinSecondaryThreshold, s => primaryThresholdValue, f =>  primaryThresholdValue + 0.1));

            var sw = await ActionTimer.MeasureAsync(async () =>
            {
                await foreach (var peckrResult in _successPoller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
                {
                    peckrResult.Outcome.Should().Be(MonitoringOutcome.Polling);
                }
            });

            var runtimeDurationInMs = runtimeDuration * 1000;
            var invocationCount = _mockRetriever.Invocations.Select(i => i.Method.Name == "GetAsync").Count();
            var estimatedCodeExecutionTime = (int) Math.Ceiling(pollingDelay * weightedExecutionTime);
            var estimatedTotalCodeExecutionTime =  estimatedCodeExecutionTime * invocationCount;

            _mockRetriever.Verify(x => x.GetAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Between(runtimeDurationInMs / (pollingDelay + estimatedCodeExecutionTime), runtimeDurationInMs / pollingDelay + 1, Range.Inclusive));

            sw.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(Math.Min(runtimeDuration, invocationCount) * 1000);
            sw.ElapsedMilliseconds.Should().BeLessOrEqualTo(Math.Max(runtimeDurationInMs + estimatedTotalCodeExecutionTime, invocationCount * pollingDelay + estimatedTotalCodeExecutionTime));
        }

        [Fact]
        public async Task Timeout_If_ShouldFailOnRunDurationExceeded_Is_True_When_Expected_Duration_Exceeded_And_Success_Condition_Is_Not_Met()
        {
            const int valueWithinSecondaryThreshold = 81;
            const int primaryThresholdValue = 60;
            const int secondaryThresholdValue = 80;
            const int runtimeDuration = 1;
            const int pollingDelay = 500;

            var settings = MonitorSettingsGenerator.Create(
                runtimeDuration,
                pollingDelay,
                primaryThresholdValue: primaryThresholdValue,
                secondaryThresholdValue: secondaryThresholdValue, 
                shouldFailOnRunDurationExceeded: true);

            _mockRetriever.Setup(x =>
                    x.GetAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MetricGenerator.OfPercentage(valueWithinSecondaryThreshold, s => primaryThresholdValue, f => primaryThresholdValue + 0.1));

            double SuccessValueProvider(int s) => 60;
            double FailureValueProvider(int s) => 62;

            _mockRetriever.SetupSequence(x =>
                    x.GetAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MetricGenerator.OfPercentage(75, SuccessValueProvider, FailureValueProvider))
                .ReturnsAsync(MetricGenerator.OfPercentage(77, SuccessValueProvider, FailureValueProvider))
                .ReturnsAsync(MetricGenerator.OfPercentage(79, SuccessValueProvider, FailureValueProvider))
                .ReturnsAsync(MetricGenerator.OfPercentage(79, SuccessValueProvider, FailureValueProvider));

            var finalOutcome = MonitoringOutcome.Polling;
            await foreach (var peckrResult in _successPoller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
            {
                finalOutcome = peckrResult.Outcome;
            }

            finalOutcome.Should().Be(MonitoringOutcome.TimedOut);
        }

        [Fact]
        public async Task Not_Timeout_If_ShouldFailOnRunDurationExceeded_Is_False_When_Expected_Duration_Exceeded_And_Success_Condition_Is_Not_Met()
        {
            const int valueWithinSecondaryThreshold = 81;
            const int primaryThresholdValue = 60;
            const int secondaryThresholdValue = 80;
            const int runtimeDuration = 1;
            const int pollingDelay = 500;

            var settings = MonitorSettingsGenerator.Create(
                runtimeDuration,
                pollingDelay,
                primaryThresholdValue: primaryThresholdValue,
                secondaryThresholdValue: secondaryThresholdValue,
                shouldFailOnRunDurationExceeded: false);

            _mockRetriever.Setup(x =>
                    x.GetAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MetricGenerator.OfPercentage(valueWithinSecondaryThreshold, s => primaryThresholdValue, f => primaryThresholdValue + 0.1));

            double SuccessValueProvider(int s) => 60;
            double FailureValueProvider(int s) => 62;

            _mockRetriever.SetupSequence(x =>
                    x.GetAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MetricGenerator.OfPercentage(75, SuccessValueProvider, FailureValueProvider))
                .ReturnsAsync(MetricGenerator.OfPercentage(77, SuccessValueProvider, FailureValueProvider))
                .ReturnsAsync(MetricGenerator.OfPercentage(79, SuccessValueProvider, FailureValueProvider))
                .ReturnsAsync(MetricGenerator.OfPercentage(79, SuccessValueProvider, FailureValueProvider));

            var finalOutcome = MonitoringOutcome.TimedOut;
            await foreach (var peckrResult in _successPoller.PollAsync(settings, CancellationToken.None).ConfigureAwait(false))
            {
                finalOutcome = peckrResult.Outcome;
            }

            finalOutcome.Should().Be(MonitoringOutcome.Polling);
        }
    }
}
