using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Peckr.Abstractions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Polly;
using Polly.Retry;
using static Peckr.Abstractions.PeckrEventSource;

namespace Peckr.Metrics.Sources.Azure
{
    public class WadPerfCountersMetricsRetriever : IPeckDataRetriever<IReadOnlyCollection<Metric>>
    {
        private static readonly Random Jitter = new Random();

        private readonly CloudStorageAccount _storageAccount;
        private readonly string _source;
        private readonly string _appId;
        private readonly IAsyncPolicy _resiliencyPolicy;

        public WadPerfCountersMetricsRetriever(string connection, string appId)
        {
            _storageAccount = CloudStorageAccount.Parse(connection);
            _source = $"{_storageAccount.Credentials.AccountName}/{WadConstants.PerformanceCountersTableName}";
            _appId = appId;
            _resiliencyPolicy = GetAsyncPolicy(_source);
        }

        public async Task<IReadOnlyCollection<Metric>> GetAsync(
            DateTimeOffset start,
            DateTimeOffset end,
            int takeLimit,
            string customFilter = null,
            bool allowFailures = true,
            CancellationToken ct = default)
        {
            var tableClient = _storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(WadConstants.PerformanceCountersTableName);

            var startTimeFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, "0" + start.Ticks);
            var endTimeFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThanOrEqual, "0" + end.Ticks);
            var filterCondition = $"{startTimeFilter} {TableOperators.And} {endTimeFilter}";
            if (customFilter != null)
            {
                filterCondition = $"{filterCondition} {TableOperators.And} {customFilter}";
            }
            var query = new TableQuery<WadPerfCounterTableEntry>()
                .Take(takeLimit)
                .Where(filterCondition);

            IReadOnlyCollection<Metric> metrics = Array.Empty<Metric>();
            var stopwatch = Stopwatch.StartNew();
            try
            {
                metrics = await RetrieveAllAsync(takeLimit, table, query, ct).ConfigureAwait(false);
            }
            catch (StorageException se) when (se.InnerException is OperationCanceledException)
            {
                throw se.InnerException;
            }
            catch (Exception ex)
            {
                Log.RetrieverUnhandledException(nameof(WadPerfCountersMetricsRetriever), ex.ToString());
                if (!allowFailures) throw;
            }
            stopwatch.Stop();

            Log.RetrieverRetrievalCompleted(
                nameof(WadPerfCountersMetricsRetriever),
                _source,
                metrics.Count,
                start.ToString(),
                end.ToString(),
                stopwatch.ElapsedMilliseconds);

            return metrics;
        }

        private async Task<IReadOnlyCollection<Metric>> RetrieveAllAsync(
            int takeLimit, CloudTable table, TableQuery<WadPerfCounterTableEntry> query, CancellationToken ct)
        {
            var metrics = new List<Metric>(takeLimit);
            TableContinuationToken token = null;
            do
            {
                var resultSegment = await _resiliencyPolicy.ExecuteAsync(
                    async () => await table.ExecuteQuerySegmentedAsync(query, token, null, null, ct).ConfigureAwait(false))
                    .ConfigureAwait(false);
                token = resultSegment.ContinuationToken;
                foreach (var wadPerfCounterTableEntry in resultSegment)
                {
                    if (metrics.Count == takeLimit)
                        break;

                    metrics.Add(wadPerfCounterTableEntry.ToMetric(_source, _appId));
                }
            } while (token != null && metrics.Count < takeLimit);
            return metrics;
        }

        private static IAsyncPolicy GetAsyncPolicy(string source)
        {
            static TimeSpan GetRetryWaitDuration(int retryAttempt) 
                => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(Jitter.Next(0, 1000));
            
            return Policy
                .Handle<StorageException>(
                    e => e.RequestInformation.HttpStatusCode == 408
                    || e.RequestInformation.HttpStatusCode == 500
                    || e.RequestInformation.HttpStatusCode == 503)
                .WaitAndRetryAsync(
                    3,
                    GetRetryWaitDuration,
                    (ex, timeSpan, retryCount, context) =>
                    {
                        Log.RetrieverRetrievalRetry(
                            nameof(WadPerfCountersMetricsRetriever), source, ex.ToString(), (int)timeSpan.TotalSeconds, retryCount);
                    });
        }
    }
}