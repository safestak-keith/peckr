using System;
using System.Linq;

namespace DiagnosticsMonitor.Logs.UnitTests.Generators
{
    public static class LogEntryGenerator
    {
        public const string DefaultSource = "prodfooapilogs";
        public const string DefaultAppId = "fooapi";
        public const LogLevel DefaultLevel = LogLevel.Error;
        public const string DefaultInstanceId = "FooAPI_0";
        public const int DefaultEventId = 4505;
        public const string DefaultMessage = "An unhandled exception was thrown";

        public static LogEntry Create(
            int seed = 0, 
            string source = DefaultSource,
            string appId = DefaultAppId,
            DateTimeOffset? timestamp = null,
            LogLevel level = DefaultLevel,
            string instanceId = DefaultInstanceId,
            int eventId = DefaultEventId,
            string operationId = null,
            string message = DefaultMessage)
        {
            return new LogEntry(
                source,
                appId,
                timestamp ?? new DateTimeOffset(2019 ,1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(-1 * seed),
                level,
                instanceId,
                eventId,
                operationId ?? $"op-{seed}",
                message);
        }

        public static LogEntry DefaultInstance = Create(0);

        public static LogEntry[] OfCount(int count)
        {
            return count == 0 
                ? Array.Empty<LogEntry>() 
                : Enumerable.Range(0, count)
                    .Select(i => Create(i))
                    .ToArray();
        }
    }
}