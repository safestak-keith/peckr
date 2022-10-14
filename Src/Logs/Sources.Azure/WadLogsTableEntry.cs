using Microsoft.WindowsAzure.Storage.Table;

namespace DiagnosticsMonitor.Logs.Sources.Azure
{
    public class WadLogsTableEntry : TableEntity
    {
        public string RoleInstance { get; set; }
        public int Level { get; set; }
        public int EventId { get; set; }
        public string Message { get; set; }
    }
}
