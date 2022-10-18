using System;
using Peckr.Abstractions;

namespace Peckr.Tests.Core.Generators
{
    public static class MonitorSettingsGenerator
    {
        public const int DefaultExpectedRunDurationSeconds = 500;
        public const int DefaultPollingDelayMilliseconds = 5000;
        public const MonitorType DefaultMonitorType = MonitorType.AzureWadLogsErrorCountUpperThreshold;
        public const int DefaultSourcePreviousSpanSeconds = 60;
        public const string DefaultSourceConnection = "DefaultEndpointsProtocol=https;AccountName=storage;AccountKey=aaaabbbbccccddddeeeeffffgggghhhhAAAABBBBCCCCDDDDEEEEFFFFGGGGHHHHaaaabbbbccccddddeeeeff==";
        public const string DefaultSourcePath = "path/to/source";
        public const string DefaultSourceFilter = "filter eq 'filter'";
        public const int DefaultSourceTakeLimit = 100;
        public const string DefaultSourceAppOrResourceId = "fooapi";
        public const int DefaultPrimaryThresholdValue = 0;
        public const int DefaultSecondaryThresholdValue = 0;
        public const bool DefaultTerminateWhenConditionMet = true;
        public const bool DefaultShouldFailOnRunDurationExceeded = false;
        public const int DefaultCooldownWhenConditionMetMilliseconds = 60000;
        public const SinkType DefaultSinkType = SinkType.None;
        public const string DefaultSinkConnection = "";
        public const string DefaultSinkPath = "";
        
        public static MonitorSettings Create(
            int expectedRunDurationSeconds = DefaultExpectedRunDurationSeconds,
            int pollingDelayMilliseconds = DefaultPollingDelayMilliseconds,
            MonitorType monitorType = DefaultMonitorType,
            int sourcePreviousSpanSeconds = DefaultSourcePreviousSpanSeconds,
            string sourceConnection = DefaultSourceConnection,
            string sourcePath = DefaultSourcePath,
            string sourceFilter= DefaultSourceFilter,
            int sourceTakeLimit = DefaultSourceTakeLimit,
            string sourceAppOrResourceId = DefaultSourceAppOrResourceId,
            int primaryThresholdValue = DefaultPrimaryThresholdValue,
            int secondaryThresholdValue = DefaultSecondaryThresholdValue,
            bool terminateWhenConditionMet = DefaultTerminateWhenConditionMet,
            bool shouldFailOnRunDurationExceeded = DefaultShouldFailOnRunDurationExceeded,
            int cooldownWhenConditionMetMilliseconds = DefaultCooldownWhenConditionMetMilliseconds,
            SinkType sinkType = DefaultSinkType,
            string sinkConnection = DefaultSinkConnection,
            string sinkPath = DefaultSinkPath)
        {
            return new MonitorSettings(
                TimeSpan.FromSeconds(expectedRunDurationSeconds),
                TimeSpan.FromMilliseconds(pollingDelayMilliseconds),
                monitorType,
                TimeSpan.FromSeconds(sourcePreviousSpanSeconds),
                sourceConnection,
                sourcePath,
                sourceFilter,
                sourceTakeLimit,
                sourceAppOrResourceId,
                primaryThresholdValue,
                secondaryThresholdValue,
                terminateWhenConditionMet,
                shouldFailOnRunDurationExceeded,
                TimeSpan.FromMilliseconds(cooldownWhenConditionMetMilliseconds),
                sinkType,
                sinkConnection,
                sinkPath);
        }
    }
}