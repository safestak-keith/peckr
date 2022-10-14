using System;
using System.Collections.Generic;
using System.Linq;
using DiagnosticsMonitor.Abstractions;

namespace DiagnosticsMonitor.ConsoleApp
{
    public class MonitorConfiguration
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
        public static MonitorSettings ToMonitorSettings(this MonitorConfiguration monitorConfiguration)
        {
            var validationErrors = monitorConfiguration.GetValidationErrors().ToArray();
            if (validationErrors.Any())
                throw new ArgumentException($"{string.Join($"{Environment.NewLine}", validationErrors)}");

            return new MonitorSettings(
                TimeSpan.FromMinutes(monitorConfiguration.DurationToRunMinutes),
                TimeSpan.FromMilliseconds(monitorConfiguration.PollingIntervalMilliseconds),
                DeriveMonitorType(monitorConfiguration.MonitorType),
                TimeSpan.FromMinutes(monitorConfiguration.SourcePreviousSpanMinutes),
                monitorConfiguration.SourceConnection,
                monitorConfiguration.SourcePath,
                monitorConfiguration.SourceFilter,
                monitorConfiguration.SourceTakeLimit,
                monitorConfiguration.SourceAppOrResourceId,
                monitorConfiguration.PrimaryThresholdValue,
                monitorConfiguration.SecondaryThresholdValue,
                monitorConfiguration.TerminateWhenConditionMet,
                monitorConfiguration.ShouldFailOnRunDurationExceeded,
                TimeSpan.FromMinutes(monitorConfiguration.CooldownWhenConditionMetMinutes),
                DeriveSinkType(monitorConfiguration.SinkType),
                monitorConfiguration.SinkConnection,
                monitorConfiguration.SinkPath);
        }

        public static IEnumerable<string> GetValidationErrors(this MonitorConfiguration monitorConfiguration)
        {
            if (monitorConfiguration.DurationToRunMinutes < 0)
                yield return $"{nameof(monitorConfiguration.DurationToRunMinutes)} must be a positive number";

            if (monitorConfiguration.PollingIntervalMilliseconds < 0)
                yield return $"{nameof(monitorConfiguration.PollingIntervalMilliseconds)} must be a positive number";
            if (monitorConfiguration.PollingIntervalMilliseconds > 86400000)
                yield return $"{nameof(monitorConfiguration.PollingIntervalMilliseconds)} must not be greater than a day";

            if (string.IsNullOrWhiteSpace(monitorConfiguration.MonitorType))
                yield return $"{nameof(monitorConfiguration.MonitorType)} must be provided";

            if (monitorConfiguration.SourcePreviousSpanMinutes < 0)
                yield return $"{nameof(monitorConfiguration.SourcePreviousSpanMinutes)} must be a positive number";

            if (string.IsNullOrWhiteSpace(monitorConfiguration.SourceConnection))
                yield return $"{nameof(monitorConfiguration.SourceConnection)} must be provided";

            const int maxTakeLimit‬ = 524288;
            if (monitorConfiguration.SourceTakeLimit < 0)
                yield return $"{nameof(monitorConfiguration.SourceTakeLimit)} must be a positive number";
            if (monitorConfiguration.SourceTakeLimit > maxTakeLimit‬)
                yield return $"{nameof(monitorConfiguration.SourceTakeLimit)} must be less than {maxTakeLimit‬}";

            if (monitorConfiguration.CooldownWhenConditionMetMinutes < 0)
                yield return $"{nameof(monitorConfiguration.CooldownWhenConditionMetMinutes)} must be a positive number";
        }

        private static MonitorType DeriveMonitorType(string monitorTypeConfig)
        {
            var monitorType = monitorTypeConfig switch
                {
                    "azwdlogs_errscnt_upperbound" => MonitorType.AzureWadLogsErrorCountUpperThreshold,
                    "azwdperf_instavg_upperbound" => MonitorType.AzureWadPerformanceCountersInstanceAverageUpperThreshold,
                    "azailogs_slot_traffic" => MonitorType.AzureAppInsightsTrafficDataMonitor,
                    "azailogs_slot_role_cpu" => MonitorType.AzureAppInsightsRoleCpuCounters,
                    "azailogs_slot_response_times" => MonitorType.AzureAppInsightsEndpointResponseTimeCounters,
                    _ => throw new ArgumentException($"{nameof(MonitorType)} is not supported"),
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