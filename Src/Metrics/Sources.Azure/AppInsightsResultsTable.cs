using System.Collections.Generic;
using System.Text.Json;

namespace DiagnosticsMonitor.Metrics.Sources.Azure
{
    public class AppInsightsResultsTable
    {
        public List<List<JsonElement>> Rows { get; set; }
    }
}
