using Peckr.Metrics.ResultEvaluators;
using Peckr.Metrics.UnitTests.Generators;
using FluentAssertions;
using Xunit;

namespace Peckr.Metrics.UnitTests.ResultEvaluators
{
    public class MetricPercentageThresholdSamplingEvaluatorShould
    {
        private MetricPercentageThresholdSamplingEvaluator _evaluator;

        [Theory]
        [InlineData(81, 60, 80)]
        [InlineData(80, 70, 80)]
        [InlineData(91, 60, 90)]
        [InlineData(90, 70, 90)]
        public void Return_True_When_IsSuccessfulResult_Condition_Is_Met(int valueWithinSecondaryThreshold, double primaryThresholdValue, double secondaryThresholdValue)
        {
            var metrics = MetricGenerator.OfPercentage(
                valueWithinSecondaryThreshold, 
                s => primaryThresholdValue,
                f => primaryThresholdValue + 0.1);

            _evaluator = new MetricPercentageThresholdSamplingEvaluator(primaryThresholdValue, secondaryThresholdValue);

            var result = _evaluator.IsConditionMet(metrics);

            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(78, 60, 80)]
        [InlineData(79, 80, 80)]
        [InlineData(80, 60, 82)]
        [InlineData(81, 80, 82)]
        public void Return_False_When_IsSuccessfulResult_Condition_Is_Not_Met(int valueNotWithinSecondaryThreshold, double primaryThresholdValue, double secondaryThresholdValue)
        {
            var metrics = MetricGenerator.OfPercentage(
                valueNotWithinSecondaryThreshold,
                s => primaryThresholdValue,
                f => primaryThresholdValue + 0.1);

            _evaluator = new MetricPercentageThresholdSamplingEvaluator(primaryThresholdValue, secondaryThresholdValue);

            var result = _evaluator.IsConditionMet(metrics);

            result.Should().BeFalse();
        }
    }
}
