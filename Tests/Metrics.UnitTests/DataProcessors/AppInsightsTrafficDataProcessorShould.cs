using System;
using System.Linq;
using System.Text.Json;
using Peckr.Metrics.DataProcessors;
using Peckr.Metrics.Sources.Azure;
using FluentAssertions;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Peckr.Metrics.UnitTests.DataProcessors
{
    public class AppInsightsTrafficDataProcessorShould
    {
        private const string DefaultAppId = "appId";
        private const string DefaultDeploymentId = "deploymentId";
        private const double DefaultValue = 1265;
        private const string DefaultSource = "app-insights";
        private const string DefaultMetricType = "requests";

        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private readonly DateTime _defaultDateTime = DateTime.Parse("2020-10-16T12:00:00Z");

        [Theory]
        [InlineData("{\"tables\": [{\"rows\": [ [\"deploymentId\",\"2020-10-16T12:00:00Z\", 1265] ]}]}", 1)]
        [InlineData("{\"tables\": [{\"rows\": [ [\"deploymentId\",\"2020-10-16T12:00:00Z\", 1265], [\"deploymentId2\",\"2020-10-17T12:00:00Z\", 1234.0] ]}]}", 2)]
        public void Return_A_Valid_List_Of_Metrics_When_Process_Is_Called(string jsonDocument, int expectedNumberOfMetrics)
        {
            var query = JsonSerializer.Deserialize<AppInsightsQueryResult>(
                jsonDocument,
                _jsonSerializerOptions
            );

            var processor = new AppInsightsTrafficDataProcessor(DefaultAppId);
            var metrics = processor.Process(query);

            metrics.Should().HaveCount(expectedNumberOfMetrics);

            var metric = metrics.ToList().First();

            metric.AppId.Should().Be(DefaultAppId);
            metric.Source.Should().Be(DefaultSource);
            metric.Name.Should().Be(DefaultDeploymentId);
            metric.Type.Should().Be(DefaultMetricType);
            metric.InstanceId.Should().Be(DefaultDeploymentId);
            metric.Value.Should().Be(DefaultValue);
            metric.Target.Should().BeNull();
            metric.Timestamp.Should().Be(_defaultDateTime);
        }

        [Theory]
        [InlineData("{\"tables\": [{\"rows\": [ [\"deploymentId\",\"2020-10-16T12:00:00Z\", \"1265\"] ]}]}")]
        [InlineData("{\"tables\": [{\"rows\": [ [126334,\"2020-10-16T12:00:00Z\", 1265] ]}]}")]
        public void Throw_An_InvalidOperationException_When_The_Json_Is_Invalid(string jsonDocument)
        {
            var query = JsonSerializer.Deserialize<AppInsightsQueryResult>(
                jsonDocument,
                _jsonSerializerOptions
            );

            var processor = new AppInsightsTrafficDataProcessor(DefaultAppId);
            Action action = () =>
            {
                var metrics = processor.Process(query).ToList();
            };

            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Throw_An_FormatException_When_The_Date_Is_Invalid()
        {
            var jsonDocument = "{\"tables\": [{\"rows\": [ [\"deploymentId\",\"testing\", 1265] ]}]}";
            var query = JsonSerializer.Deserialize<AppInsightsQueryResult>(
                jsonDocument,
                _jsonSerializerOptions
            );

            var processor = new AppInsightsTrafficDataProcessor(DefaultAppId);
            Action action = () =>
            {
                var metrics = processor.Process(query).ToList();
            };

            action.Should().Throw<FormatException>();
        }
    }
}
