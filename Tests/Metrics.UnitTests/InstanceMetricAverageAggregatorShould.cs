using System;
using System.Collections.Generic;
using System.Linq;
using DiagnosticsMonitor.Metrics.UnitTests.Generators;
using FluentAssertions;
using Xunit;

namespace DiagnosticsMonitor.Metrics.UnitTests
{
    [Collection("UnitTestFixtures")]
    public class InstanceMetricAverageAggregatorShould
    {
        [Theory]
        [InlineData(1, 0)]
        [InlineData(10, 15)]
        [InlineData(15, 82)]
        public void Calculate_Instance_Level_Average_Metrics_Correctly_Given_The_Same_Instance_Grouping_Properties_And_Values(
            int metricCount, double metricValue)
        {
            var metrics = MetricGenerator.OfCount(metricCount, i => metricValue);

            var instanceAverageMetrics = metrics.AggregateInstanceAverageMetrics(
                "MyApp",
                new DateTimeOffset(2019,1,1, 0, 0, 0, TimeSpan.Zero));

            instanceAverageMetrics.Single().Value.Should().Be(metricValue);
        }

        [Theory]
        [InlineData("0,1,2,3,4")]
        [InlineData("10,54,35")]
        [InlineData("19460,22346")]
        public void Calculate_Instance_Level_Average_Metrics_Correctly_Given_The_Same_Instance_Grouping_Properties_And_Different_Values(
            string metricValuesCsv)
        {
            var metricValueStrings = metricValuesCsv.Split(",");
            var metricValues = metricValueStrings.Select(int.Parse);
            var metrics = metricValues.Select(
                    value => MetricGenerator.Create(instanceId: MetricGenerator.DefaultInstanceId, valueProvider: _ => value))
                .ToArray();

            var instanceAverageMetrics = metrics.AggregateInstanceAverageMetrics(
                "MyApp",
                new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero));

            instanceAverageMetrics.Single().Value.Should().Be(metricValues.Average());
        }

        [Theory]
        [InlineData(1, "MyApi", 2019, 1, 1, 0, 0, 0)]
        [InlineData(10, "MyWorker", 2019, 12, 31, 23, 59, 59)]
        [InlineData(15, "MyFunction", 2019, 8, 7, 17, 23, 41)]
        public void Roll_Up_To_One_Metric_With_The_Same_AppId_And_Timestamp_Given_The_Same_Instance_Grouping_Properties(
            int metricCount, string appId, int year, int month, int day, int hour, int minute, int second)
        {
            var timestamp = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
            var metrics = MetricGenerator.OfCount(metricCount, i => 10);

            var instanceAverageMetrics = metrics.AggregateInstanceAverageMetrics(appId, timestamp);

            instanceAverageMetrics.Length.Should().Be(1);
            instanceAverageMetrics.All(m => m.AppId == appId).Should().BeTrue();
            instanceAverageMetrics.All(m => m.Timestamp == timestamp).Should().BeTrue();
        }

        [Theory]
        [InlineData("storage/WADPerformanceCountersTable", "PerfCounter", "Processor\\% Processor Time", null, "API_0")]
        [InlineData("app-insights/dependencies", "HTTP", "GET", "www.safestak.com", "APP_1")]
        public void Roll_Up_To_One_Metric_With_The_Same_Grouping_Properties_Given_The_Same_Instance_Grouping_Properties(
            string source, string metricType, string metricName, string metricTarget, string instanceId)
        {
            var metrics = new[]
            {
                MetricGenerator.Create(source: source, type: metricType, name:metricName, target:metricTarget, instanceId:instanceId, valueProvider:_ => 10),
                MetricGenerator.Create(source: source, type: metricType, name:metricName, target:metricTarget, instanceId:instanceId, valueProvider:_ => 54),
                MetricGenerator.Create(source: source, type: metricType, name:metricName, target:metricTarget, instanceId:instanceId, valueProvider:_ => 35),
            };

            var instanceAverageMetrics = metrics.AggregateInstanceAverageMetrics(
                "API", new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero));

            instanceAverageMetrics.Length.Should().Be(1);
            instanceAverageMetrics.All(m => m.Source == source).Should().BeTrue();
            instanceAverageMetrics.All(m => m.Type == metricType).Should().BeTrue();
            instanceAverageMetrics.All(m => m.Name == metricName).Should().BeTrue();
            instanceAverageMetrics.All(m => m.Target == metricTarget).Should().BeTrue();
            instanceAverageMetrics.All(m => m.InstanceId == instanceId).Should().BeTrue();
        }

        [Fact]
        public void Roll_Up_To_Multiple_Metrics_With_The_Correct_Grouping_Properties_Given_Multiple_Instance_Grouping_Properties()
        {
            var metrics = new[]
            {
                MetricGenerator.Create(source: "app-insights/dependencies", type: "Type1", name: "Name1", target: "Target1", instanceId: "Instance1", valueProvider:_ => 10),
                MetricGenerator.Create(source: "storage/WADPerformanceCountersTable", type: "Type2", name: "Name2", target: "Target2", instanceId: "Instance2", valueProvider:_ => 54),
                MetricGenerator.Create(source: "storage/WADPerformanceCountersTable", type: "Type2", name: "Name2", target: "Target2", instanceId: "Instance2", valueProvider:_ => 35),
                MetricGenerator.Create(source: "app-insights/dependencies", type: "Type1", name: "Name1", target: "Target1", instanceId: "Instance1", valueProvider:_ => 34),
                MetricGenerator.Create(source: "storage/WADPerformanceCountersTable", type: "Type2", name: "Name2", target: "Target2", instanceId: "Instance2", valueProvider:_ => 10),
                MetricGenerator.Create(source: "app-insights/customMetrics", type: "Type3", name: "Name3", target: "Target3", instanceId: "Instance3", valueProvider:_ => 44),
            };

            var instanceAverageMetrics = metrics.AggregateInstanceAverageMetrics(
                "API", new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero));

            instanceAverageMetrics.Length.Should().Be(3);
            var aiDepsInstance1 = instanceAverageMetrics.First(
                m => m.Source == "app-insights/dependencies" && m.Type == "Type1" && m.Name == "Name1" && m.Target == "Target1" && m.InstanceId == "Instance1");
            var wadPerfInstance2 = instanceAverageMetrics.First(
                m => m.Source == "storage/WADPerformanceCountersTable" && m.Type == "Type2" && m.Name == "Name2" && m.Target == "Target2" && m.InstanceId == "Instance2");
            var aiCustomInstance3 = instanceAverageMetrics.First(
                m => m.Source == "app-insights/customMetrics" && m.Type == "Type3" && m.Name == "Name3" && m.Target == "Target3" && m.InstanceId == "Instance3");
            aiDepsInstance1.Value.Should().Be(22);
            wadPerfInstance2.Value.Should().Be(33);
            aiCustomInstance3.Value.Should().Be(44);
        }

        [Fact]
        public void Return_Empty_For_Empty_Input()
        {
            var instanceAverageMetrics = new List<Metric>().AggregateInstanceAverageMetrics(
                "API", new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero));

            instanceAverageMetrics.Should().BeSameAs(Array.Empty<Metric>());
        }

        [Fact]
        public void Throw_Argument_Null_Exception_For_Null_Input()
        {
            List<Metric> metrics = null;

            Action nullInputAggregationAction = () => metrics.AggregateInstanceAverageMetrics(
                "API", new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero));

            nullInputAggregationAction.Should().Throw<ArgumentNullException>();
        }
    }
}