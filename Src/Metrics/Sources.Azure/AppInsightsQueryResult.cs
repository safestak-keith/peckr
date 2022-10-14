using System.Collections.Generic;

namespace DiagnosticsMonitor.Metrics.Sources.Azure
{
    public class AppInsightsQueryResult
    {
        public List<AppInsightsResultsTable> Tables { get; set; }
    }
}