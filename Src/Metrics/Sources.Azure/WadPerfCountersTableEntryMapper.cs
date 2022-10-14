namespace DiagnosticsMonitor.Metrics.Sources.Azure
{
    public static class WadPerfCountersTableEntryMapper
    {
        private const string PerfCounterType = "PerfCounter";

        public static Metric ToMetric(this WadPerfCounterTableEntry entry, string source, string appId)
        {
            return new Metric(
                source,
                appId,
                entry.Timestamp,
                PerfCounterType,
                entry.CounterName,
                null,
                entry.CounterValue,
                entry.RoleInstance,
                null);
        }
    }
}
