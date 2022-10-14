using System.Collections.Generic;
using System.Linq;
using DiagnosticsMonitor.Abstractions;

namespace DiagnosticsMonitor.Metrics.ResultEvaluators
{
    public class MetricPercentageThresholdSamplingEvaluator : IMonitoringResultEvaluator<IReadOnlyCollection<Metric>>
    {
        private readonly double _primaryThresholdValue;
        private readonly double _secondaryThresholdValue;

        public MetricPercentageThresholdSamplingEvaluator(double primaryThresholdValue, double secondaryThresholdValue)
        {
            _primaryThresholdValue = primaryThresholdValue;
            _secondaryThresholdValue = secondaryThresholdValue;
        }

        public bool IsConditionMet(IReadOnlyCollection<Metric> metrics)
        {
            var metricsWithinThresholdCount = metrics.Count(metric => metric.Value <= _primaryThresholdValue);
            var metricsWithThresholdCount = (double)metricsWithinThresholdCount / metrics.Count * 100;
            var isConditionMet = metricsWithThresholdCount >= _secondaryThresholdValue;

            metrics.Last().Trace = $"Metrics within primary threshold ({_primaryThresholdValue}): {metricsWithinThresholdCount}/{metrics.Count}\n" +
                                   $"Percentage of metrics within primary threshold: {metricsWithThresholdCount}\n" +
                                   $"Required percentage threshold: {_secondaryThresholdValue}\n" +
                                   $"Criteria Met: {isConditionMet}";
            return isConditionMet;
        }
    }
}
