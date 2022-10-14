using Microsoft.WindowsAzure.Storage.Table;

namespace DiagnosticsMonitor.Metrics.Sources.Azure
{
    public class WadPerfCounterTableEntry : TableEntity
    {
        public string RoleInstance { get; set; }
        public string CounterName { get; set; }
        public double CounterValue { get; set; }
    }
}
