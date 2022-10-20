using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Peckr.Abstractions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Polly;
using static Peckr.Abstractions.PeckrEventSource;

namespace Peckr.Logs.Sources.Azure
{
    public class WadLogsErrorsRetriever : IPeckDataRetriever<IReadOnlyCollection<LogEntry>>
    {
        private static readonly Random Jitter = new Random();

        private readonly CloudStorageAccount _storageAccount;
        private readonly string _source;
        private readonly string _appId;
        private readonly IAsyncPolicy _policy;

        public WadLogsErrorsRetriever(string connection, string appId)
        {
            _storageAccount = CloudStorageAccount.Parse(connection);
            _source = $"{_storageAccount.Credentials.AccountName}/{WadConstants.LogsTableName}";
            _appId = appId;
            _policy = GetAsyncPolicy(_source);
        }

        public async Task<IReadOnlyCollection<LogEntry>> GetAsync(
            DateTimeOffset start,
            DateTimeOffset end,
            int takeLimit,
            string customFilter = null,
            bool allowFailures = true,
            CancellationToken ct = default)
        {
            var tableClient = _storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(WadConstants.LogsTableName);

            var startTimeFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, "0" + start.Ticks);
            var endTimeFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThanOrEqual, "0" + end.Ticks);
            var partitionKeyFilter = $"{startTimeFilter} {TableOperators.And} {endTimeFilter}";
            var levelFilter = TableQuery.GenerateFilterConditionForInt("Level", QueryComparisons.LessThanOrEqual, WadConstants.ErrorLevel);
            var filterCondition = $"{partitionKeyFilter} {TableOperators.And} {levelFilter}";
            if (customFilter != null)
            {
                filterCondition = $"{filterCondition} {TableOperators.And} {customFilter}";
            }
            var query = new TableQuery<WadLogsTableEntry>()
                .Take(takeLimit)
                .Where(filterCondition);

            IReadOnlyCollection<LogEntry> logs = Array.Empty<LogEntry>();
            var stopwatch = Stopwatch.StartNew();
            try
            {
                logs = await RetrieveAllAsync(takeLimit, table, query, ct).ConfigureAwait(false);
            }
            catch (StorageException se) when (se.InnerException is OperationCanceledException)
            {
                throw se.InnerException;
            }
            catch (Exception ex)
            {
                Log.RetrieverUnhandledException(nameof(WadLogsErrorsRetriever), ex.ToString());
                if (!allowFailures) throw;
            }
            stopwatch.Stop();

            Log.RetrieverRetrievalCompleted(
                nameof(WadLogsErrorsRetriever),
                _source,
                logs.Count,
                start.ToString(),
                end.ToString(),
                stopwatch.ElapsedMilliseconds);

            return logs;
        }

        private async Task<IReadOnlyCollection<LogEntry>> RetrieveAllAsync(
            int takeLimit, CloudTable table, TableQuery<WadLogsTableEntry> query, CancellationToken ct)
        {
            var logs = new List<LogEntry>(takeLimit);
            TableContinuationToken token = null;
            do
            {
                var resultSegment = await _policy.ExecuteAsync(
                    async () => await table.ExecuteQuerySegmentedAsync(query, token, null, null, ct).ConfigureAwait(false))
                    .ConfigureAwait(false);
                token = resultSegment.ContinuationToken;
                foreach (var wadLogEntry in resultSegment)
                {
                    if (logs.Count == takeLimit)
                        break;

                    logs.Add(wadLogEntry.ToLogEntry(_source, _appId));
                }
            } while (token != null && logs.Count < takeLimit);
            return logs;
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
                            nameof(WadLogsErrorsRetriever), source, ex.ToString(), (int)timeSpan.TotalSeconds, retryCount);
                    });
        }
    }
}