using System.Collections.Generic;

namespace Peckr.ConsoleApp
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
                { DurationToRunMinutes, $"{nameof(PeckrConfiguration.DurationToRunMinutes)}" },
                { PollingIntervalMilliseconds, $"{nameof(PeckrConfiguration.PollingIntervalMilliseconds)}" },
                { MonitorType, $"{nameof(PeckrConfiguration.MonitorType)}" },
                { SourceTakeLimit, $"{nameof(PeckrConfiguration.SourceTakeLimit)}" },
                { SourcePreviousSpanMinutes, $"{nameof(PeckrConfiguration.SourcePreviousSpanMinutes)}" },
                { SourceConnection, $"{nameof(PeckrConfiguration.SourceConnection)}" },
                { SourceFilter, $"{nameof(PeckrConfiguration.SourceFilter)}" },
                { SourceAppOrResourceId, $"{nameof(PeckrConfiguration.SourceAppOrResourceId)}" },
                { PrimaryThresholdValue, $"{nameof(PeckrConfiguration.PrimaryThresholdValue)}" },
                { SecondaryThresholdValue, $"{nameof(PeckrConfiguration.SecondaryThresholdValue)}" },
                { TerminateWhenConditionMet, $"{nameof(PeckrConfiguration.TerminateWhenConditionMet)}" },
                { ShouldFailOnRunDurationExceeded, $"{nameof(PeckrConfiguration.ShouldFailOnRunDurationExceeded)}" },
                { CooldownWhenConditionMetMinutes, $"{nameof(PeckrConfiguration.CooldownWhenConditionMetMinutes)}" },
                { SinkType, $"{nameof(PeckrConfiguration.SinkType)}" },
                { SinkConnection, $"{nameof(PeckrConfiguration.SinkConnection)}" },
            };
    }
}
