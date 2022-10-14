using System;
using System.Collections.Generic;
using DiagnosticsMonitor.Abstractions;
using DiagnosticsMonitor.ConsoleApp.Sinks;
using DiagnosticsMonitor.Logs;
using DiagnosticsMonitor.Logs.Sinks.Slack;
using DiagnosticsMonitor.Logs.Sources.Azure;
using DiagnosticsMonitor.Metrics;
using DiagnosticsMonitor.Metrics.DataProcessors;
using DiagnosticsMonitor.Metrics.ResultEvaluators;
using DiagnosticsMonitor.Metrics.Sinks.Slack;
using DiagnosticsMonitor.Metrics.Sources.Azure;

namespace DiagnosticsMonitor.ConsoleApp
{
    /// <summary>
    /// Basic factory until increase in complexity requires a more sophisticated DI approach
    /// </summary>
    public static class MonitorFactory
    {
        public static IConsoleMonitor CreateMonitor(MonitorSettings settings)
        {
            switch (settings.MonitorType)
            {
                case MonitorType.AzureWadPerformanceCountersInstanceAverageUpperThreshold:
                    var metricRetriever = new WadPerfCountersMetricsRetriever(settings.SourceConnection, settings.SourceAppOrResourceId);
                    var metricPoller = new InstanceMetricAverageUpperThresholdPoller(metricRetriever);
                    var metricSink = GetMetricMonitoringResultSink(settings);
                    return new PollingConsoleMonitor<IReadOnlyCollection<Metric>>(metricPoller, metricSink);
                case MonitorType.AzureWadLogsErrorCountUpperThreshold:
                    var logRetriever = new WadLogsErrorsRetriever(
                        settings.SourceConnection, settings.SourceAppOrResourceId);
                    var logPoller = new LogCountUpperThresholdPoller(logRetriever);
                    var logSink = GetLogMonitoringResultSink(settings);
                    return new PollingConsoleMonitor<IReadOnlyCollection<LogEntry>>(logPoller, logSink);
                case MonitorType.AzureAppInsightsTrafficDataMonitor: 
                    var trafficDataProcessor = new AppInsightsTrafficDataProcessor(settings.SourceAppOrResourceId);
                    var trafficMetricsRetriever = new AppInsightsMetricsRetriever(settings.SourceConnection, settings.SourceAppOrResourceId, trafficDataProcessor);
                    var trafficMetricsEvaluator = new MetricPercentageThresholdSamplingEvaluator(settings.PrimaryThresholdValue, settings.SecondaryThresholdValue);
                    var trafficMetricsPoller = new MetricsPoller(trafficMetricsRetriever, trafficMetricsEvaluator);
                    var trafficMetricSink = GetMetricMonitoringResultSink(settings);
                    return new PollingConsoleMonitor<IReadOnlyCollection<Metric>>(trafficMetricsPoller, trafficMetricSink);
                case MonitorType.AzureAppInsightsRoleCpuCounters:
                    var cpuDataProcessor = new AppInsightsCpuDataProcessor(settings.SourceAppOrResourceId);
                    var cpuMetricsRetriever = new AppInsightsMetricsRetriever(settings.SourceConnection, settings.SourceAppOrResourceId, cpuDataProcessor);
                    var cpuMetricsEvaluator = new MetricSingleUpperThresholdEvaluator(settings.PrimaryThresholdValue);
                    var cpuMetricsPoller = new MetricsPoller(cpuMetricsRetriever, cpuMetricsEvaluator, false);
                    var cpuMetricSink = GetMetricMonitoringResultSink(settings);
                    return new PollingConsoleMonitor<IReadOnlyCollection<Metric>>(cpuMetricsPoller, cpuMetricSink);
                case MonitorType.AzureAppInsightsEndpointResponseTimeCounters:
                    var rtDataProcessor = new AppInsightsResponseTimeDataProcessor(settings.SourceAppOrResourceId);
                    var rtMetricsRetriever = new AppInsightsMetricsRetriever(settings.SourceConnection, settings.SourceAppOrResourceId, rtDataProcessor);
                    var rtMetricsEvaluator = new MetricSingleUpperThresholdEvaluator(settings.PrimaryThresholdValue);
                    var rtMetricsPoller = new MetricsPoller(rtMetricsRetriever, rtMetricsEvaluator, false);
                    var rtMetricSink = GetMetricMonitoringResultSink(settings);
                    return new PollingConsoleMonitor<IReadOnlyCollection<Metric>>(rtMetricsPoller, rtMetricSink);
                default:
                    throw new InvalidOperationException($"Unsupported MonitorType {settings.MonitorType}");
            }
        }

        private static IMonitoringResultSink<IReadOnlyCollection<Metric>> GetMetricMonitoringResultSink(
            MonitorSettings settings)
        {
            return settings.SinkType switch
            {
                SinkType.SlackWebHook => new MetricConsoleSink(new InstanceMetricThresholdExceededSlackSink(settings.SinkConnection)),
                _ => new MetricConsoleSink(VoidMonitoringResultSink<IReadOnlyCollection<Metric>>.Instance),
            };
        }

        private static IMonitoringResultSink<IReadOnlyCollection<LogEntry>> GetLogMonitoringResultSink(
            MonitorSettings settings)
        {
            return settings.SinkType switch
            {
                SinkType.SlackWebHook => new LogConsoleSink(new ErrorLogsSlackSink(settings.SinkConnection)),
                _ => new LogConsoleSink(VoidMonitoringResultSink<IReadOnlyCollection<LogEntry>>.Instance),
            };
        }
    }
}