using System;
using System.Linq;
using System.Text.Json;
using Peckr.Metrics.DataProcessors;
using Peckr.Metrics.Sources.Azure;
using FluentAssertions;
using Xunit;

namespace Peckr.Metrics.UnitTests.DataProcessors
{
    public class AppInsightsResponseTimeDataProcessorShould
    {
        private const string DefaultAppId = "appId";
        private const string DefaultDeploymentId = "deploymentId";
        private const string DefaultEndpointName = "endpointName";
        private const double DefaultValue = 42;
        private const string DefaultSource = "app-insights";
        private const string DefaultMetricType = "performanceCounters";

        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private readonly DateTime _defaultDateTime = DateTime.Parse("2020-10-16T12:00:00Z");

        [Theory]
        [InlineData("{\"tables\": [{\"rows\": [ [\"2020-10-16T12:00:00Z\", \"endpointName\", \"deploymentId\", 42] ]}]}", 1)]
        [InlineData("{\"tables\": [{\"rows\": [ [\"2020-10-16T12:00:00Z\",\"endpointName\", \"deploymentId\", 42], [\"2020-10-17T12:00:00Z\",\"endpointName2\", \"deploymentId2\", 97.979797] ]}]}", 2)]
        public void Return_A_Valid_List_Of_Metrics_When_Process_Is_Called(string jsonDocument, int expectedNumberOfMetrics)
        {
            var query = JsonSerializer.Deserialize<AppInsightsQueryResult>(
                jsonDocument,
                _jsonSerializerOptions
            );

            var processor = new AppInsightsResponseTimeDataProcessor(DefaultAppId);
            var metrics = processor.Process(query);

            metrics.Should().HaveCount(expectedNumberOfMetrics);

            var metric = metrics.ToList().First();

            metric.AppId.Should().Be(DefaultAppId);
            metric.Source.Should().Be(DefaultSource);
            metric.Name.Should().Be(nameof(AppInsightsResponseTimeDataProcessor));
            metric.Type.Should().Be(DefaultMetricType);
            metric.InstanceId.Should().Be(DefaultEndpointName);
            metric.Value.Should().Be(DefaultValue);
            metric.Target.Should().Be(DefaultDeploymentId);
            metric.Timestamp.Should().Be(_defaultDateTime);
        }

        [Theory]
        [InlineData("{\"tables\": [{\"rows\": [  [\"2020-10-16T12:00:00Z\", 2345, \"deploymentId\", 42] ]}]}")]
        [InlineData("{\"tables\": [{\"rows\": [  [\"2020-10-16T12:00:00Z\", \"endpointName\", 132, 42] ]}]}")]
        [InlineData("{\"tables\": [{\"rows\": [  [\"2020-10-16T12:00:00Z\", \"endpointName\", \"deploymentId\", \"42\"] ]}]}")]
        public void Throw_An_InvalidOperationException_When_The_Json_Is_Invalid(string jsonDocument)
        {
            var query = JsonSerializer.Deserialize<AppInsightsQueryResult>(
                jsonDocument,
                _jsonSerializerOptions
            );

            var processor = new AppInsightsResponseTimeDataProcessor(DefaultAppId);
            Action action = () =>
            {
                var metrics = processor.Process(query).ToList();
            };

            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Throw_An_FormatException_When_The_Date_Is_Invalid()
        {
            var jsonDocument = "{\"tables\": [{\"rows\": [  [\"2020-10-16T12:00xxxxZ\", \"roleInstanceName\", \"deploymentId\", 42] ]}]}";
            var query = JsonSerializer.Deserialize<AppInsightsQueryResult>(
                jsonDocument,
                _jsonSerializerOptions
            );

            var processor = new AppInsightsResponseTimeDataProcessor(DefaultAppId);
            Action action = () =>
            {
                var metrics = processor.Process(query).ToList();
            };

            action.Should().Throw<FormatException>();
        }
    }
}
