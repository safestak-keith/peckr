using System.Collections.Generic;

namespace Peckr.Metrics.Sources.Azure
{
    public class AppInsightsQueryResult
    {
        public List<AppInsightsResultsTable> Tables { get; set; }
    }
}