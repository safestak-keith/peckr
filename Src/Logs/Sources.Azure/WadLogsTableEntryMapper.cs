namespace DiagnosticsMonitor.Logs.Sources.Azure
{
    public static class WadLogsTableEntryMapper
    {
        public static LogEntry ToLogEntry(this WadLogsTableEntry entry, string source, string appId)
        {
            return new LogEntry(
                source,
                appId,
                entry.Timestamp,
                (LogLevel)entry.Level,
                entry.RoleInstance,
                entry.EventId,
                null,
                entry.Message);
        }
    }
}
