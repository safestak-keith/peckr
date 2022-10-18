using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Peckr.Abstractions;
using Polly;
using Slack.Webhooks;
using static Peckr.Abstractions.PeckrEventSource;

namespace Peckr.Metrics.Sinks.Slack
{
    public class InstanceMetricThresholdExceededSlackSink : IMonitoringResultSink<IReadOnlyCollection<Metric>>
    {
        private static readonly Random Jitter = new Random();

        private readonly SlackClient _slackClient;
        private readonly IAsyncPolicy<bool> _policy;

        public InstanceMetricThresholdExceededSlackSink(string webHookUrl)
        {
            _slackClient = new SlackClient(webHookUrl);
            _policy = GetAsyncPolicy(webHookUrl);
        }

        public async ValueTask PushMonitoringResultAsync(
            MonitoringResult<IReadOnlyCollection<Metric>> result,
            MonitorSettings settings,
            CancellationToken ct)
        {
            var failures = result.Result
                .Where(m => m.Value > settings.PrimaryThresholdValue)
                .OrderByDescending(m => m.Value)
                .ToArray();
            if (result.Outcome != MonitoringOutcome.Failure || !failures.Any())
                return;

            var message = GenerateSlackMessage(settings, failures);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                _ = await _policy.ExecuteAsync(
                        async () => await _slackClient.PostAsync(message).ConfigureAwait(false))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.SinkUnhandledException(nameof(InstanceMetricThresholdExceededSlackSink), ex.ToString());
            }
            finally
            {
                Log.SinkPushCompleted(
                    nameof(InstanceMetricThresholdExceededSlackSink), "Slack Webhook", failures.Length, stopwatch.ElapsedMilliseconds);
            }
        }

        private static SlackMessage GenerateSlackMessage(MonitorSettings settings, Metric[] failures)
        {
            const int maxDisplayCount = 50;
            var failuresToDisplay = failures.Take(maxDisplayCount).ToArray();
            var firstMetric = failures.First();
            var title = string.IsNullOrWhiteSpace(firstMetric.AppId)
                ? $"*{firstMetric.Source}*{Environment.NewLine}"
                : $"*{firstMetric.AppId}* ({firstMetric.Source}){Environment.NewLine}";
            var sb = new StringBuilder(title);
            var countText = (failures.Length > maxDisplayCount)
                ? $"{maxDisplayCount}+"
                : $"{failures.Length}";
            sb.AppendLine($"{countText} instance(s) found with `{firstMetric.Name}` values greater than threshold of `{settings.PrimaryThresholdValue}` over the past `{settings.SourcePreviousSpan.TotalMinutes}` minutes\n");
            foreach (var instanceMetric in failuresToDisplay)
            {
                sb.AppendLine($"*{instanceMetric.InstanceId}* - {instanceMetric.Value:N}");
            }

            return new SlackMessage
            {
                Text = sb.ToString(),
                Markdown = true,
            };
        }

        private static IAsyncPolicy<bool> GetAsyncPolicy(string destination)
        {
            const int retryCount = 3;
            const int maxJitterMilliseconds = 1000;

            static TimeSpan GetRetryWaitDuration(int retryAttempt)
                => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(Jitter.Next(0, maxJitterMilliseconds));
            
            return Policy<bool>
                .Handle<HttpRequestException>()
                .OrResult(false)
                .WaitAndRetryAsync(
                    retryCount,
                    GetRetryWaitDuration,
                    (ex, timeSpan, retryCount, context) =>
                    {
                        Log.SinkPushRetry(
                            nameof(InstanceMetricThresholdExceededSlackSink), destination, ex.ToString(), (int)timeSpan.TotalSeconds, retryCount);
                    });
        }
    }
}