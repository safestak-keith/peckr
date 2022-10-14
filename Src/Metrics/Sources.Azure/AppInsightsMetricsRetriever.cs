using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using DiagnosticsMonitor.Abstractions;
using System.Text.Json;
using Polly;
using static DiagnosticsMonitor.Abstractions.DiagnosticsMonitorEventSource;

namespace DiagnosticsMonitor.Metrics.Sources.Azure
{
    public class AppInsightsMetricsRetriever : IMonitorDataRetriever<IReadOnlyCollection<Metric>>
    {
        private const string Source = "app-insights";

        private readonly HttpClient _client;
        private readonly string _appId;
        private readonly IDataProcessor<AppInsightsQueryResult, IEnumerable<Metric>> _dataProcessor;

        private static IAsyncPolicy _policy;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};

        public AppInsightsMetricsRetriever(string sourceConnection, string appId, IDataProcessor<AppInsightsQueryResult, IEnumerable<Metric>> dataProcessor)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Add("x-api-key", sourceConnection);
            _appId = appId;
            _dataProcessor = dataProcessor;
            _policy = GetAsyncPolicy();
        }

        public async Task<IReadOnlyCollection<Metric>> GetAsync(
            DateTimeOffset start, 
            DateTimeOffset end, 
            int takeLimit, 
            string customFilter = null,
            bool allowFailures = true,
            CancellationToken ct = default
        )
        {
            IReadOnlyCollection<Metric> metrics = Array.Empty<Metric>();

            var minInterval = (int) end.Subtract(start).TotalMinutes;
            var timespan = $"timespan=PT{minInterval}M";

            var stopwatch = Stopwatch.StartNew();
            try
            {
                metrics = await RetrieveAllAsync(takeLimit, customFilter, timespan,ct);
            }
            catch (Exception ex)
            {
                Log.RetrieverUnhandledException(nameof(AppInsightsMetricsRetriever), ex.ToString());
                if (!allowFailures) throw;
            }
            stopwatch.Stop();

            Log.RetrieverRetrievalCompleted(
                nameof(AppInsightsMetricsRetriever),
                Source,
                metrics.Count,
                start.ToString(),
                end.ToString(),
                stopwatch.ElapsedMilliseconds);

            return metrics;
        }

        private async Task<IReadOnlyCollection<Metric>> RetrieveAllAsync(
            int takeLimit, 
            string query, 
            string timespan, 
            CancellationToken ct
        )
        {
            var metrics = new List<Metric>();
            var encodedQuery = Uri.EscapeUriString(query);
            var requestUri = $"https://api.applicationinsights.io/v1/apps/{_appId}/query?{timespan}&query={encodedQuery}";

            var resultCountThreshold = Math.Abs(takeLimit);
            var sequentialEmptyResultsCounter = 0;

            do 
            {
                metrics.Clear();

                var response = await _policy.ExecuteAsync(async () => await _client.GetAsync(requestUri, ct));
                if (!response.IsSuccessStatusCode)
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.Forbidden:
                        case HttpStatusCode.NotFound:
                        case HttpStatusCode.BadRequest:
                            throw new ApplicationException($"[{response.StatusCode}] Please check your request: {Uri.EscapeUriString(response.RequestMessage.RequestUri.ToString())}");
                        default:
                            continue;
                    }
                }

                var stringResponse = await response.Content.ReadAsStringAsync();
                
                var queryResult = JsonSerializer.Deserialize<AppInsightsQueryResult>(stringResponse, _jsonSerializerOptions);
                var resultsTable = queryResult?.Tables?.FirstOrDefault();
                if (resultsTable == null || resultsTable.Rows.Count < 1)
                {
                    Log.RetrieverRetrievalNoResults(nameof(AppInsightsMetricsRetriever), query);
                    sequentialEmptyResultsCounter++;

                    if (sequentialEmptyResultsCounter > resultCountThreshold)
                    {
                        throw new ApplicationException($"No results returned after {resultCountThreshold} successive attempts, please check your configuration and query:\n{HttpUtility.UrlDecode(query)}");
                    }
                    continue;
                }

                sequentialEmptyResultsCounter = 0;
                
                try
                {
                    metrics.AddRange(_dataProcessor.Process(queryResult));
                }
                catch (Exception e)
                {
                    throw new ApplicationException($"Error processing response, please check your request: {Uri.EscapeUriString(response.RequestMessage.RequestUri.ToString())}", e);
                }

            } while (metrics.Count < resultCountThreshold);

            if (resultCountThreshold == 0) return metrics;

            return takeLimit > 0  
                ? metrics.Take(resultCountThreshold).ToList() 
                : metrics.TakeLast(resultCountThreshold).ToList();
        }

        private static IAsyncPolicy GetAsyncPolicy()
        {
            return Policy.Handle<SocketException>()
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(2)
                });
        }
    }
}