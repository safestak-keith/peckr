using System.Collections.Generic;
using System.Linq;
using DiagnosticsMonitor.Abstractions;
using DiagnosticsMonitor.Metrics.Sources.Azure;

namespace DiagnosticsMonitor.Metrics.DataProcessors
{
    public class AppInsightsCpuDataProcessor : IDataProcessor<AppInsightsQueryResult, IEnumerable<Metric>>
    {
        private const string Source = "app-insights";
        private const string MetricType = "performanceCounters";
        private const string CounterName = nameof(AppInsightsCpuDataProcessor);
        private readonly string _appId;

        public AppInsightsCpuDataProcessor(string appId)
        {
            _appId = appId;
        }

        public IEnumerable<Metric> Process(AppInsightsQueryResult result)
        {
            return result.Tables.First().Rows.Select(r =>
            {
                var timestamp = r[0].GetDateTime();
                var roleInstanceName = r[1].GetString();
                var deploymentId = r[2].GetString();
                var cpuValue = r[3].GetDouble();

                return new Metric(Source, _appId, timestamp, MetricType, CounterName, deploymentId, cpuValue, roleInstanceName);
            });
        }
    }
}
