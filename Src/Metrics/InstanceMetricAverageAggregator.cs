using System;
using System.Collections.Generic;
using System.Linq;

namespace DiagnosticsMonitor.Metrics
{
    public static class InstanceMetricAverageAggregator
    {
        public static Metric[] AggregateInstanceAverageMetrics(
            this IReadOnlyCollection<Metric> results, string appOrResourceId, DateTimeOffset timestamp)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            
            if (!results.Any()) return Array.Empty<Metric>();

            return results
                .GroupBy(m => (instanceId: m.InstanceId, metricSource: m.Source, metricType: m.Type, metricName: m.Name, metricTarget: m.Target))
                .Select(
                    instanceMetrics => new Metric(
                        instanceMetrics.Key.metricSource,
                        appOrResourceId,
                        timestamp,
                        instanceMetrics.Key.metricType,
                        instanceMetrics.Key.metricName,
                        instanceMetrics.Key.metricTarget,
                        instanceMetrics.Average(m => m.Value),
                        instanceMetrics.Key.instanceId))
                .ToArray();
        }
    }
}