using System;
using System.Collections.Generic;
using Peckr.Abstractions;
using Peckr.ConsoleApp.Sinks;
using Peckr.Logs;
using Peckr.Logs.Sinks.Slack;
using Peckr.Logs.Sources.Azure;
using Peckr.Metrics;
using Peckr.Metrics.DataProcessors;
using Peckr.Metrics.ResultEvaluators;
using Peckr.Metrics.Sinks.Slack;
using Peckr.Metrics.Sources.Azure;

namespace Peckr.ConsoleApp
{
    /// <summary>
    /// Basic factory until increase in complexity requires a more sophisticated DI approach
    /// </summary>
    public static class PeckrFactory
    {
        public static IConsolePeckr CreatePeckr(PeckrSettings settings)
        {
            switch (settings.MonitorType)
            {
                case PeckrType.AzureWadPerformanceCountersInstanceAverageUpperThreshold:
                    var metricRetriever = new WadPerfCountersMetricsRetriever(settings.SourceConnection, settings.SourceAppOrResourceId);
                    var metricPoller = new InstanceMetricAverageUpperThresholdPoller(metricRetriever);
                    var metricSink = GetMetricMonitoringResultSink(settings);
                    return new PollingConsolePeckr<IReadOnlyCollection<Metric>>(metricPoller, metricSink);
                case PeckrType.AzureWadLogsErrorCountUpperThreshold:
                    var logRetriever = new WadLogsErrorsRetriever(
                        settings.SourceConnection, settings.SourceAppOrResourceId);
                    var logPoller = new LogCountUpperThresholdPoller(logRetriever);
                    var logSink = GetLogMonitoringResultSink(settings);
                    return new PollingConsolePeckr<IReadOnlyCollection<LogEntry>>(logPoller, logSink);
                case PeckrType.AzureAppInsightsTrafficDataMonitor: 
                    var trafficDataProcessor = new AppInsightsTrafficDataProcessor(settings.SourceAppOrResourceId);
                    var trafficMetricsRetriever = new AppInsightsMetricsRetriever(settings.SourceConnection, settings.SourceAppOrResourceId, trafficDataProcessor);
                    var trafficMetricsEvaluator = new MetricPercentageThresholdSamplingEvaluator(settings.PrimaryThresholdValue, settings.SecondaryThresholdValue);
                    var trafficMetricsPoller = new MetricsPoller(trafficMetricsRetriever, trafficMetricsEvaluator);
                    var trafficMetricSink = GetMetricMonitoringResultSink(settings);
                    return new PollingConsolePeckr<IReadOnlyCollection<Metric>>(trafficMetricsPoller, trafficMetricSink);
                case PeckrType.AzureAppInsightsRoleCpuCounters:
                    var cpuDataProcessor = new AppInsightsCpuDataProcessor(settings.SourceAppOrResourceId);
                    var cpuMetricsRetriever = new AppInsightsMetricsRetriever(settings.SourceConnection, settings.SourceAppOrResourceId, cpuDataProcessor);
                    var cpuMetricsEvaluator = new MetricSingleUpperThresholdEvaluator(settings.PrimaryThresholdValue);
                    var cpuMetricsPoller = new MetricsPoller(cpuMetricsRetriever, cpuMetricsEvaluator, false);
                    var cpuMetricSink = GetMetricMonitoringResultSink(settings);
                    return new PollingConsolePeckr<IReadOnlyCollection<Metric>>(cpuMetricsPoller, cpuMetricSink);
                case PeckrType.AzureAppInsightsEndpointResponseTimeCounters:
                    var rtDataProcessor = new AppInsightsResponseTimeDataProcessor(settings.SourceAppOrResourceId);
                    var rtMetricsRetriever = new AppInsightsMetricsRetriever(settings.SourceConnection, settings.SourceAppOrResourceId, rtDataProcessor);
                    var rtMetricsEvaluator = new MetricSingleUpperThresholdEvaluator(settings.PrimaryThresholdValue);
                    var rtMetricsPoller = new MetricsPoller(rtMetricsRetriever, rtMetricsEvaluator, false);
                    var rtMetricSink = GetMetricMonitoringResultSink(settings);
                    return new PollingConsolePeckr<IReadOnlyCollection<Metric>>(rtMetricsPoller, rtMetricSink);
                default:
                    throw new InvalidOperationException($"Unsupported MonitorType {settings.MonitorType}");
            }
        }

        private static IPeckResultSink<IReadOnlyCollection<Metric>> GetMetricMonitoringResultSink(
            PeckrSettings settings)
        {
            return settings.SinkType switch
            {
                SinkType.SlackWebHook => new MetricConsoleSink(new InstanceMetricThresholdExceededSlackSink(settings.SinkConnection)),
                _ => new MetricConsoleSink(VoidPeckResultSink<IReadOnlyCollection<Metric>>.Instance),
            };
        }

        private static IPeckResultSink<IReadOnlyCollection<LogEntry>> GetLogMonitoringResultSink(
            PeckrSettings settings)
        {
            return settings.SinkType switch
            {
                SinkType.SlackWebHook => new LogConsoleSink(new ErrorLogsSlackSink(settings.SinkConnection)),
                _ => new LogConsoleSink(VoidPeckResultSink<IReadOnlyCollection<LogEntry>>.Instance),
            };
        }
    }
}