using System.Collections.Generic;
using System.Linq;
using Peckr.Abstractions;
using Peckr.Metrics.Sources.Azure;

namespace Peckr.Metrics.DataProcessors
{
    public class AppInsightsResponseTimeDataProcessor : IDataProcessor<AppInsightsQueryResult, IEnumerable<Metric>>
    {
        private const string Source = "app-insights";
        private const string MetricType = "performanceCounters";
        private const string CounterName = nameof(AppInsightsResponseTimeDataProcessor);
        private readonly string _appId;

        public AppInsightsResponseTimeDataProcessor(string appId)
        {
            _appId = appId;
        }

        public IEnumerable<Metric> Process(AppInsightsQueryResult result)
        {
            return result.Tables.First().Rows.Select(r =>
            {
                var timestamp = r[0].GetDateTime();
                var endpointName = r[1].GetString();
                var deploymentId = r[2].GetString();
                var rtValue = r[3].GetDouble();

                return new Metric(Source, _appId, timestamp, MetricType, CounterName, deploymentId, rtValue, endpointName);
            });
        }
    }
}
