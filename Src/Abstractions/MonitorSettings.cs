using System;

namespace DiagnosticsMonitor.Abstractions
{
    public enum MonitorType
    {
        AzureWadLogsErrorCountUpperThreshold,
        AzureWadPerformanceCountersInstanceAverageUpperThreshold,
        AzureAppInsightsTrafficDataMonitor,
        AzureAppInsightsRoleCpuCounters,
        AzureAppInsightsEndpointResponseTimeCounters
        //AzureAppInsightsTraces,
        //AzureAppInsightsExceptions,
        //AzureAppInsightsDependencies,
    }

    public enum SinkType
    {
        None,
        SlackWebHook,
        //AzureTableStorage,
        AzureAppInsightsMetrics
        //AzureAppInsightsDependencies
    }

    public class MonitorSettings
    {
        public TimeSpan ExpectedRunDuration { get; }
        public TimeSpan PollingDelay { get; }
        public MonitorType MonitorType { get; }
        public TimeSpan SourcePreviousSpan { get; }
        public string SourceConnection { get; }
        public string SourcePath { get; }
        public string SourceFilter { get; }
        public int SourceTakeLimit { get; }
        public string SourceAppOrResourceId { get; }
        public double PrimaryThresholdValue { get; }
        public double SecondaryThresholdValue { get; }
        public bool TerminateWhenConditionMet { get; }
        public bool ShouldFailOnRunDurationExceeded { get; }
        public TimeSpan CooldownPeriodWhenConditionMet { get; }
        public SinkType SinkType { get; }
        public string SinkConnection { get; }
        public string SinkPath{ get; }

        public MonitorSettings(
            TimeSpan expectedRunDuration, 
            TimeSpan pollingDelay, 
            MonitorType monitorType,
            TimeSpan sourcePreviousSpan, 
            string sourceConnection, 
            string sourcePath, 
            string sourceFilter,
            int sourceTakeLimit, 
            string sourceAppOrResourceId, 
            double primaryThresholdValue,
            double secondaryThresholdValue,
            bool terminateWhenConditionMet,
            bool shouldFailOnRunDurationExceeded,
            TimeSpan cooldownPeriodWhenConditionMet, 
            SinkType sinkType,
            string sinkConnection, 
            string sinkPath)
        {
            ExpectedRunDuration = expectedRunDuration;
            PollingDelay = pollingDelay;
            MonitorType = monitorType;
            SourcePreviousSpan = sourcePreviousSpan;
            SourceConnection = sourceConnection ?? throw new ArgumentNullException(nameof(sourceConnection));
            SourcePath = sourcePath;
            SourceFilter = sourceFilter;
            SourceTakeLimit = sourceTakeLimit;
            SourceAppOrResourceId = sourceAppOrResourceId;
            PrimaryThresholdValue = primaryThresholdValue;
            SecondaryThresholdValue = secondaryThresholdValue;
            TerminateWhenConditionMet = terminateWhenConditionMet;
            ShouldFailOnRunDurationExceeded = shouldFailOnRunDurationExceeded;
            CooldownPeriodWhenConditionMet = cooldownPeriodWhenConditionMet;
            SinkType = sinkType;
            SinkConnection = sinkConnection;
            SinkPath = sinkPath;
        }
    }
}