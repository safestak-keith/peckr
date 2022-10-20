using Peckr.Abstractions;
using Peckr.ConsoleApp.Sinks;
using Peckr.Logs;
using Peckr.Logs.Sinks.Slack;
using Peckr.Metrics;
using Peckr.Metrics.Sinks.Slack;
using Peckr.Tests.Core.Generators;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Peckr.ConsoleApp.UnitTests
{
    [Collection("UnitTestFixtures")]
    public class MonitorFactoryShould
    {
        [Fact]
        public void Create_Monitor_With_Correct_Poller_Dependency_Given_AzureWadPerformanceCountersInstanceAverageUpperThreshold_MonitorType()
        {
            var settings = MonitorSettingsGenerator.Create(
                peckrType: PeckrType.AzureWadPerformanceCountersInstanceAverageUpperThreshold);

            var peckr = PeckrFactory.CreatePeckr(settings);

            var peckrPoller = typeof(PollingConsolePeckr<IReadOnlyCollection<Metric>>)
                .GetField("_poller", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(peckr);
            peckrPoller.Should().BeOfType<InstanceMetricAverageUpperThresholdPoller>();
        }

        [Fact]
        public void Create_Monitor_With_Correct_Poller_Dependency_Given_AzureWadLogsErrorCountUpperThreshold_MonitorType()
        {
            var settings = MonitorSettingsGenerator.Create(
                peckrType: PeckrType.AzureWadLogsErrorCountUpperThreshold);

            var peckr = PeckrFactory.CreatePeckr(settings);

            var peckrPoller = typeof(PollingConsolePeckr<IReadOnlyCollection<LogEntry>>)
                .GetField("_poller", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(peckr);
            peckrPoller.Should().BeOfType<LogCountUpperThresholdPoller>();
        }

        [Fact]
        public void Create_Monitor_With_Correct_Sink_Dependencies_Given_SlackWebHook_SinkType_And_AzureWadLogsErrorCountUpperThreshold_MonitorType()
        {
            var settings = MonitorSettingsGenerator.Create(
                peckrType: PeckrType.AzureWadLogsErrorCountUpperThreshold,
                sinkType: SinkType.SlackWebHook, sinkConnection: "https://hooks.slack.com/services/A1A1A1A1A/B2B2B2B2B/abcd1234abcd1234abcd1234");

            var peckr = PeckrFactory.CreatePeckr(settings);

            var decoratorSink = typeof(PollingConsolePeckr<IReadOnlyCollection<LogEntry>>)
                .GetField("_resultSink", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(peckr);
            var slackSink = typeof(LogConsoleSink)
                .GetField("_decoratedSink", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(decoratorSink);
            decoratorSink.Should().BeOfType<LogConsoleSink>();
            slackSink.Should().BeOfType<ErrorLogsSlackSink>();
        }

        [Fact]
        public void Create_Monitor_With_Correct_Sink_Dependencies_Given_SinkType_Of_None_And_AzureWadLogsErrorCountUpperThreshold_MonitorType()
        {
            var settings = MonitorSettingsGenerator.Create(
                peckrType: PeckrType.AzureWadLogsErrorCountUpperThreshold,
                sinkType: SinkType.None);

            var peckr = PeckrFactory.CreatePeckr(settings);

            var decoratorSink = typeof(PollingConsolePeckr<IReadOnlyCollection<LogEntry>>)
                .GetField("_resultSink", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(peckr);
            var voidSink = typeof(LogConsoleSink)
                .GetField("_decoratedSink", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(decoratorSink);
            decoratorSink.Should().BeOfType<LogConsoleSink>();
            voidSink.Should().BeOfType<VoidPeckResultSink<IReadOnlyCollection<LogEntry>>>();
        }

        [Fact]
        public void Create_Monitor_With_Correct_Sink_Dependencies_Given_SlackWebHook_SinkType_And_AzureWadPerformanceCountersInstanceAverageUpperThreshold_MonitorType()
        {
            var settings = MonitorSettingsGenerator.Create(
                peckrType: PeckrType.AzureWadPerformanceCountersInstanceAverageUpperThreshold,
                sinkType: SinkType.SlackWebHook, sinkConnection: "https://hooks.slack.com/services/A1A1A1A1A/B2B2B2B2B/abcd1234abcd1234abcd1234");

            var peckr = PeckrFactory.CreatePeckr(settings);

            var decoratorSink = typeof(PollingConsolePeckr<IReadOnlyCollection<Metric>>)
                .GetField("_resultSink", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(peckr);
            var slackSink = typeof(MetricConsoleSink)
                .GetField("_decoratedSink", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(decoratorSink);
            decoratorSink.Should().BeOfType<MetricConsoleSink>();
            slackSink.Should().BeOfType<InstanceMetricThresholdExceededSlackSink>();
        }

        [Fact]
        public void Create_Monitor_With_Correct_Sink_Dependencies_Given_SinkType_Of_None_And_AzureWadPerformanceCountersInstanceAverageUpperThreshold_MonitorType()
        {
            var settings = MonitorSettingsGenerator.Create(
                peckrType: PeckrType.AzureWadPerformanceCountersInstanceAverageUpperThreshold,
                sinkType: SinkType.None);

            var peckr = PeckrFactory.CreatePeckr(settings);

            var decoratorSink = typeof(PollingConsolePeckr<IReadOnlyCollection<Metric>>)
                .GetField("_resultSink", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(peckr);
            var voidSink = typeof(MetricConsoleSink)
                .GetField("_decoratedSink", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(decoratorSink);
            decoratorSink.Should().BeOfType<MetricConsoleSink>();
            voidSink.Should().BeOfType<VoidPeckResultSink<IReadOnlyCollection<Metric>>>();
        }
    }
}
