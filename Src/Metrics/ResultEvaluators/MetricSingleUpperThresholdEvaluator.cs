using System.Collections.Generic;
using System.Linq;
using DiagnosticsMonitor.Abstractions;

namespace DiagnosticsMonitor.Metrics.ResultEvaluators
{
    public class MetricSingleUpperThresholdEvaluator : IMonitoringResultEvaluator<IReadOnlyCollection<Metric>>
    {
        private readonly double _primaryThresholdValue;

        public MetricSingleUpperThresholdEvaluator(double primaryThresholdValue)
        {
            _primaryThresholdValue = primaryThresholdValue;
        }

        public bool IsConditionMet(IReadOnlyCollection<Metric> metrics)
        {
            return metrics.All(metric => metric.Value > _primaryThresholdValue);
        }
    }
}
