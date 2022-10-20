using System;
using Peckr.Metrics.ResultEvaluators;
using FluentAssertions;
using Xunit;

namespace Peckr.Metrics.UnitTests.ResultEvaluators
{
    public class MetricSingleUpperThresholdEvaluatorShould
    {
        [Theory]
        [InlineData(20, 19.99999999)]
        [InlineData(20, 20)]
        [InlineData(10, 9.99999999)]
        [InlineData(10, 10)]
        public void Return_False_When_Metric_Is_Within_UpperThreshold_And_IsCriteriaMet_Is_Called(double singleThresholdValue, double metricValue)
        {
            var evaluator = new MetricSingleUpperThresholdEvaluator(singleThresholdValue);
            var metrics = new[] {new Metric("", "", DateTimeOffset.UtcNow, "", "", "", metricValue, "")};

            var result = evaluator.IsConditionMet(metrics);

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(20, 20.0000001)]
        [InlineData(20, 21)]
        [InlineData(10, 10.0000001)]
        [InlineData(10, 11)]
        public void Return_True_When_Metric_Is_Not_Within_UpperThreshold_And_IsCriteriaMet_Is_Called(double singleThresholdValue, double metricValue)
        {
            var evaluator = new MetricSingleUpperThresholdEvaluator(singleThresholdValue);
            var metrics = new[] { new Metric("", "", DateTimeOffset.UtcNow, "", "", "", metricValue, "") };

            var result = evaluator.IsConditionMet(metrics);

            result.Should().BeTrue();
        }
    }
}
