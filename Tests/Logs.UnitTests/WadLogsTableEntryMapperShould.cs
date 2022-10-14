using System;
using DiagnosticsMonitor.Logs.Sources.Azure;
using FluentAssertions;
using Xunit;

namespace DiagnosticsMonitor.Logs.UnitTests
{
    [Collection("UnitTestFixtures")]
    public class WadLogsTableEntryMapperShould
    {   
        [Theory]
        [InlineData("source1", "app1", 1, "Instance1", "NullReferenceException", 3)]
        [InlineData("source2", "app2", 2, "Instance2", "Exception", 4)]
        public void Map_Fields_Correctly(string source, string appId, int level, string instanceId, string message, int eventId)
        {
            var timestamp = new DateTimeOffset(2019, 5, 24, 0, 0, 0, TimeSpan.FromSeconds(0));
            var mappedResult = new WadLogsTableEntry()
            {
                Level = level,
                Timestamp = timestamp,
                RoleInstance = instanceId,
                EventId = eventId,
                Message = message,
            }.ToLogEntry(source, appId);

            mappedResult.Source.Should().Be(source);
            mappedResult.AppId.Should().Be(appId);
            mappedResult.Timestamp.Should().Be(timestamp);
            mappedResult.Level.Should().Be((LogLevel)level);
            mappedResult.InstanceId.Should().Be(instanceId);
            mappedResult.Message.Should().Be(message);
            mappedResult.EventId.Should().Be(eventId);
        }
    }
}
