using System;
using System.Collections.Generic;
using System.Linq;
using Peckr.Abstractions;

namespace Peckr.ConsoleApp
{
    public class PeckrConfiguration
    {
        public int DurationToRunMinutes { get; set; } = 10;
        public int PollingIntervalMilliseconds { get; set; } = 5000;
        public string MonitorType { get; set; } // azwdlogs_errscnt_upperbound, azwdperf_instavg_upperbound, TODO: azailogs_* or azaiperf_*
        public int SourcePreviousSpanMinutes { get; set; } = 5;
        public string SourceConnection { get; set; }
        public string SourcePath { get; set; }
        public int SourceTakeLimit { get; set; } = 10;
        public string SourceFilter { get; set; }
        public string SourceAppOrResourceId { get; set; }
        public int PrimaryThresholdValue { get; set; }
        public int SecondaryThresholdValue { get; set; }
        public int CooldownWhenConditionMetMinutes { get; set; } = 5;
        public bool TerminateWhenConditionMet { get; set; } = true;
        public bool ShouldFailOnRunDurationExceeded { get; set; } = false;
        public string SinkType { get; set; } // slackwebhook TODO: aztbstor, azaimetrics, azaideps
        public string SinkPath { get; set; }
        public string SinkConnection { get; set; }
    }

    public static class MonitorConfigurationExtensions
    {
        public static PeckrSettings ToMonitorSettings(this PeckrConfiguration peckrConfig)
        {
            var validationErrors = peckrConfig.GetValidationErrors().ToArray();
            if (validationErrors.Any())
                throw new ArgumentException($"{string.Join($"{Environment.NewLine}", validationErrors)}");

            return new PeckrSettings(
                TimeSpan.FromMinutes(peckrConfig.DurationToRunMinutes),
                TimeSpan.FromMilliseconds(peckrConfig.PollingIntervalMilliseconds),
                DeriveMonitorType(peckrConfig.MonitorType),
                TimeSpan.FromMinutes(peckrConfig.SourcePreviousSpanMinutes),
                peckrConfig.SourceConnection,
                peckrConfig.SourcePath,
                peckrConfig.SourceFilter,
                peckrConfig.SourceTakeLimit,
                peckrConfig.SourceAppOrResourceId,
                peckrConfig.PrimaryThresholdValue,
                peckrConfig.SecondaryThresholdValue,
                peckrConfig.TerminateWhenConditionMet,
                peckrConfig.ShouldFailOnRunDurationExceeded,
                TimeSpan.FromMinutes(peckrConfig.CooldownWhenConditionMetMinutes),
                DeriveSinkType(peckrConfig.SinkType),
                peckrConfig.SinkConnection,
                peckrConfig.SinkPath);
        }

        public static IEnumerable<string> GetValidationErrors(this PeckrConfiguration peckrConfig)
        {
            if (peckrConfig.DurationToRunMinutes < 0)
                yield return $"{nameof(peckrConfig.DurationToRunMinutes)} must be a positive number";

            if (peckrConfig.PollingIntervalMilliseconds < 0)
                yield return $"{nameof(peckrConfig.PollingIntervalMilliseconds)} must be a positive number";
            if (peckrConfig.PollingIntervalMilliseconds > 86400000)
                yield return $"{nameof(peckrConfig.PollingIntervalMilliseconds)} must not be greater than a day";

            if (string.IsNullOrWhiteSpace(peckrConfig.MonitorType))
                yield return $"{nameof(peckrConfig.MonitorType)} must be provided";

            if (peckrConfig.SourcePreviousSpanMinutes < 0)
                yield return $"{nameof(peckrConfig.SourcePreviousSpanMinutes)} must be a positive number";

            if (string.IsNullOrWhiteSpace(peckrConfig.SourceConnection))
                yield return $"{nameof(peckrConfig.SourceConnection)} must be provided";

            const int maxTakeLimit‬ = 524288;
            if (peckrConfig.SourceTakeLimit < 0)
                yield return $"{nameof(peckrConfig.SourceTakeLimit)} must be a positive number";
            if (peckrConfig.SourceTakeLimit > maxTakeLimit‬)
                yield return $"{nameof(peckrConfig.SourceTakeLimit)} must be less than {maxTakeLimit‬}";

            if (peckrConfig.CooldownWhenConditionMetMinutes < 0)
                yield return $"{nameof(peckrConfig.CooldownWhenConditionMetMinutes)} must be a positive number";
        }

        private static PeckrType DeriveMonitorType(string monitorTypeConfig)
        {
            var monitorType = monitorTypeConfig switch
                {
                    "azwdlogs_errscnt_upperbound" => PeckrType.AzureWadLogsErrorCountUpperThreshold,
                    "azwdperf_instavg_upperbound" => PeckrType.AzureWadPerformanceCountersInstanceAverageUpperThreshold,
                    "azailogs_slot_traffic" => PeckrType.AzureAppInsightsTrafficDataMonitor,
                    "azailogs_slot_role_cpu" => PeckrType.AzureAppInsightsRoleCpuCounters,
                    "azailogs_slot_response_times" => PeckrType.AzureAppInsightsEndpointResponseTimeCounters,
                    _ => throw new ArgumentException($"{nameof(PeckrType)} is not supported"),
                };
            return monitorType;
        }

        private static SinkType DeriveSinkType(string sinkTypeConfig)
        {
            var sinkType = SinkType.None;
            if (sinkTypeConfig != null)
            {
                sinkType = sinkTypeConfig switch
                    {
                        "slackwebhook" => SinkType.SlackWebHook,
                        _ => throw new ArgumentException($"{nameof(SinkType)} is not supported"),
                    };
            }

            return sinkType;
        }
    }
}