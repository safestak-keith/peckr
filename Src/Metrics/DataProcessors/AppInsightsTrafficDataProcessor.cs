using System.Collections.Generic;
using System.Linq;
using Peckr.Abstractions;
using Peckr.Metrics.Sources.Azure;

namespace Peckr.Metrics.DataProcessors
{
    public class AppInsightsTrafficDataProcessor : IDataProcessor<AppInsightsQueryResult, IEnumerable<Metric>>
    {
        private const string Source = "app-insights";
        private const string MetricType = "requests";
        private readonly string _appId;

        public AppInsightsTrafficDataProcessor(string appId)
        {
            _appId = appId;
        }

        public IEnumerable<Metric> Process(AppInsightsQueryResult result)
        {
            return result.Tables.First().Rows.Select(r =>
            {
                var deploymentId = r[0].GetString();
                var timestamp = r[1].GetDateTime();
                var requestsPerSecond = r[2].GetDouble();

                return new Metric(Source, _appId, timestamp, MetricType, deploymentId, null, requestsPerSecond, deploymentId);
            });
        }
    }
}
