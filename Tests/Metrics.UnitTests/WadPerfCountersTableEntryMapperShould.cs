using System;
using DiagnosticsMonitor.Metrics.Sources.Azure;
using FluentAssertions;
using Xunit;

namespace DiagnosticsMonitor.Metrics.UnitTests
{
    [Collection("UnitTestFixtures")]
    public class WadPerfCountersTableEntryMapperShould
    {   
        [Theory]
        [InlineData("source1", "app1", "\\Processor(_Total)\\% Processor Time", 25.5, "Instance1")]
        [InlineData("source2", "app2", "\\Memory\\Available MBytes", 4000, "Instance2")]
        public void Map_Fields_Correctly(
            string source, string appId, string counterName, double counterValue, string instanceId)
        {
            var timestamp = new DateTimeOffset(2019, 5, 24, 0, 0, 0, TimeSpan.FromSeconds(0));
            var mappedResult = new WadPerfCounterTableEntry()
            {
                Timestamp = timestamp,
                RoleInstance = instanceId,
                CounterName = counterName,
                CounterValue = counterValue,
            }.ToMetric(source, appId);

            mappedResult.Source.Should().Be(source);
            mappedResult.AppId.Should().Be(appId);
            mappedResult.Timestamp.Should().Be(timestamp);
            mappedResult.Name.Should().Be(counterName);
            mappedResult.Value.Should().Be(counterValue);
            mappedResult.InstanceId.Should().Be(instanceId);
        }
    }
}
