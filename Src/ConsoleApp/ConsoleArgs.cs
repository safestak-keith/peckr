using System.Collections.Generic;

namespace DiagnosticsMonitor.ConsoleApp
{
    public static class ConsoleArgs
    {
        public const string DurationToRunMinutes = "-d";
        public const string PollingIntervalMilliseconds = "-i";
        public const string MonitorType = "-m";
        public const string SourceTakeLimit = "-l";
        public const string SourcePreviousSpanMinutes = "-p";
        public const string SourceConnection = "-c";
        public const string SourceFilter = "-f";
        public const string SourceAppOrResourceId = "-a";
        public const string PrimaryThresholdValue = "-v";
        public const string SecondaryThresholdValue = "-u";
        public const string TerminateWhenConditionMet = "-t";
        public const string ShouldFailOnRunDurationExceeded = "-e";
        public const string CooldownWhenConditionMetMinutes = "-w";
        public const string SinkType = "-n";
        public const string SinkConnection = "-o";

        public static readonly Dictionary<string, string> Mappings =
            new Dictionary<string, string>
            {
                { DurationToRunMinutes, $"{nameof(MonitorConfiguration.DurationToRunMinutes)}" },
                { PollingIntervalMilliseconds, $"{nameof(MonitorConfiguration.PollingIntervalMilliseconds)}" },
                { MonitorType, $"{nameof(MonitorConfiguration.MonitorType)}" },
                { SourceTakeLimit, $"{nameof(MonitorConfiguration.SourceTakeLimit)}" },
                { SourcePreviousSpanMinutes, $"{nameof(MonitorConfiguration.SourcePreviousSpanMinutes)}" },
                { SourceConnection, $"{nameof(MonitorConfiguration.SourceConnection)}" },
                { SourceFilter, $"{nameof(MonitorConfiguration.SourceFilter)}" },
                { SourceAppOrResourceId, $"{nameof(MonitorConfiguration.SourceAppOrResourceId)}" },
                { PrimaryThresholdValue, $"{nameof(MonitorConfiguration.PrimaryThresholdValue)}" },
                { SecondaryThresholdValue, $"{nameof(MonitorConfiguration.SecondaryThresholdValue)}" },
                { TerminateWhenConditionMet, $"{nameof(MonitorConfiguration.TerminateWhenConditionMet)}" },
                { ShouldFailOnRunDurationExceeded, $"{nameof(MonitorConfiguration.ShouldFailOnRunDurationExceeded)}" },
                { CooldownWhenConditionMetMinutes, $"{nameof(MonitorConfiguration.CooldownWhenConditionMetMinutes)}" },
                { SinkType, $"{nameof(MonitorConfiguration.SinkType)}" },
                { SinkConnection, $"{nameof(MonitorConfiguration.SinkConnection)}" },
            };
    }
}
