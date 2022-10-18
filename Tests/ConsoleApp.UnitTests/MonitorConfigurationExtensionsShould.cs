using Peckr.Abstractions;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Peckr.ConsoleApp.UnitTests
{
    [Collection("UnitTestFixtures")]
    public class MonitorConfigurationExtensionsShould
    {
        [Theory]
        [InlineData(1, 1000, "azwdlogs_errscnt_upperbound", 10, "sc1", "sp1", "sf1", 10, "sa1", 0, 0, true, 1, null, null, null, MonitorType.AzureWadLogsErrorCountUpperThreshold, SinkType.None)]
        [InlineData(5, 8000, "azwdperf_instavg_upperbound", 15, "sc2", "sp2", "sf2", 99, "sa2", 9, 6, false, 5, "slackwebhook", "sc2", "sp2", MonitorType.AzureWadPerformanceCountersInstanceAverageUpperThreshold, SinkType.SlackWebHook)]
        public void Map_Everything_Correctly_Given_Valid_Configuration_When_Mapping_ToMonitorSettings(
            int durationToRunMinutes,
            int pollingIntervalMilliseconds,
            string monitorType,
            int sourcePreviousSpanMinutes,
            string sourceConnection,
            string sourcePath,
            string sourceFilter,
            int sourceTakeLimit,
            string sourceAppOrResourceId,
            int primaryThresholdValue,
            int secondaryThresholdValue,
            bool terminateWhenConditionMet,
            int cooldownWhenConditionMetMinutes,
            string sinkType,
            string sinkConnection,
            string sinkPath,
            MonitorType expectedMonitorType,
            SinkType expectedSinkType)
        {
            var configuration = new MonitorConfiguration
            {
                DurationToRunMinutes = durationToRunMinutes,
                PollingIntervalMilliseconds = pollingIntervalMilliseconds,
                MonitorType = monitorType,
                SourcePreviousSpanMinutes = sourcePreviousSpanMinutes,
                SourceConnection = sourceConnection,
                SourcePath = sourcePath,
                SourceFilter = sourceFilter,
                SourceTakeLimit = sourceTakeLimit,
                SourceAppOrResourceId = sourceAppOrResourceId,
                PrimaryThresholdValue = primaryThresholdValue,
                SecondaryThresholdValue = secondaryThresholdValue,
                TerminateWhenConditionMet = terminateWhenConditionMet,
                CooldownWhenConditionMetMinutes = cooldownWhenConditionMetMinutes,
                SinkType = sinkType,
                SinkConnection = sinkConnection,
                SinkPath = sinkPath
            };

            var settings = configuration.ToMonitorSettings();

            settings.ExpectedRunDuration.Should().Be(TimeSpan.FromMinutes(configuration.DurationToRunMinutes));
            settings.PollingDelay.Should().Be(TimeSpan.FromMilliseconds(configuration.PollingIntervalMilliseconds));
            settings.MonitorType.Should().Be(expectedMonitorType);
            settings.SourcePreviousSpan.Should().Be(TimeSpan.FromMinutes(configuration.SourcePreviousSpanMinutes));
            settings.SourceConnection.Should().Be(sourceConnection);
            settings.SourcePath.Should().Be(sourcePath);
            settings.SourceFilter.Should().Be(sourceFilter);
            settings.SourceTakeLimit.Should().Be(sourceTakeLimit);
            settings.SourceAppOrResourceId.Should().Be(sourceAppOrResourceId);
            settings.PrimaryThresholdValue.Should().Be(primaryThresholdValue);
            settings.SecondaryThresholdValue.Should().Be(secondaryThresholdValue);
            settings.TerminateWhenConditionMet.Should().Be(terminateWhenConditionMet);
            settings.CooldownPeriodWhenConditionMet.Should().Be(TimeSpan.FromMinutes(cooldownWhenConditionMetMinutes));
            settings.SinkType.Should().Be(expectedSinkType);
            settings.SinkConnection.Should().Be(sinkConnection);
            settings.SinkPath.Should().Be(sinkPath);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_Mandatory_Properties_When_Calling_GetValidationErrors(string value)
        {
            var configuration = new MonitorConfiguration
            {
                DurationToRunMinutes = 10,
                PollingIntervalMilliseconds = 1000,
                MonitorType = value,
                SourcePreviousSpanMinutes = 15,
                SourceConnection = value,
                SourcePath = "sp",
                SourceFilter = "sf",
                SourceTakeLimit = 100,
                SourceAppOrResourceId = "sa",
                PrimaryThresholdValue = 10,
                TerminateWhenConditionMet = false,
                CooldownWhenConditionMetMinutes = 1,
                SinkType = null,
                SinkConnection = null,
                SinkPath = null
            };

            var validationErrors = new HashSet<string>(configuration.GetValidationErrors());

            validationErrors.Contains($"{nameof(configuration.MonitorType)} must be provided").Should().BeTrue();
            validationErrors.Contains($"{nameof(configuration.SourceConnection)} must be provided").Should().BeTrue();
        }

        [Fact]
        public void Validate_Positive_Numbers_For_Durations_And_Limits_When_Calling_GetValidationErrors()
        {
            var configuration = new MonitorConfiguration
            {
                DurationToRunMinutes = -1,
                PollingIntervalMilliseconds = -1,
                MonitorType = "azwdlogs_errscnt_upperbound",
                SourcePreviousSpanMinutes = -1,
                SourceConnection = "sc",
                SourcePath = "sp",
                SourceFilter = "sf",
                SourceTakeLimit = -1,
                SourceAppOrResourceId = "sa",
                PrimaryThresholdValue = 10,
                SecondaryThresholdValue = 8,
                TerminateWhenConditionMet = false,
                CooldownWhenConditionMetMinutes = -1,
                SinkType = null,
                SinkConnection = null,
                SinkPath = null
            };

            var validationErrors = new HashSet<string>(configuration.GetValidationErrors());

            validationErrors.Contains($"{nameof(configuration.DurationToRunMinutes)} must be a positive number").Should().BeTrue();
            validationErrors.Contains($"{nameof(configuration.PollingIntervalMilliseconds)} must be a positive number").Should().BeTrue();
            validationErrors.Contains($"{nameof(configuration.SourcePreviousSpanMinutes)} must be a positive number").Should().BeTrue();
            validationErrors.Contains($"{nameof(configuration.SourceTakeLimit)} must be a positive number").Should().BeTrue();
            validationErrors.Contains($"{nameof(configuration.CooldownWhenConditionMetMinutes)} must be a positive number").Should().BeTrue();
        }
    }
}
