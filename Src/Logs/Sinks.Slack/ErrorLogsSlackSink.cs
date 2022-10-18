using Peckr.Abstractions;
using Polly;
using Slack.Webhooks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Peckr.Abstractions.PeckrEventSource;

namespace Peckr.Logs.Sinks.Slack
{
    public class ErrorLogsSlackSink : IMonitoringResultSink<IReadOnlyCollection<LogEntry>>
    {
        private static readonly Random Jitter = new Random();

        private readonly SlackClient _slackClient;
        private readonly IAsyncPolicy<bool> _policy;

        public ErrorLogsSlackSink(string webHookUrl)
        {
            _slackClient = new SlackClient(webHookUrl);
            _policy = GetAsyncPolicy(webHookUrl);
        }

        public async ValueTask PushMonitoringResultAsync(
            MonitoringResult<IReadOnlyCollection<LogEntry>> result,
            MonitorSettings settings,
            CancellationToken ct)
        {
            var errors = result.Result.Where(r => r.Level <= LogLevel.Error).ToArray();
            if (result.Outcome != MonitoringOutcome.Failure || !errors.Any())
                return;
            
            var message = GenerateSlackMessage(settings, errors);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                _ = await _policy.ExecuteAsync(
                        async () => await _slackClient.PostAsync(message).ConfigureAwait(false))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.SinkUnhandledException(nameof(ErrorLogsSlackSink), ex.ToString());
            }
            finally
            {
                Log.SinkPushCompleted(
                    nameof(ErrorLogsSlackSink), "Slack Webhook", errors.Length, stopwatch.ElapsedMilliseconds);
            }
        }

        private static SlackMessage GenerateSlackMessage(MonitorSettings settings, LogEntry[] errors)
        {
            const int maxDisplayCount = 5;
            var failuresToDisplay = errors.OrderBy(l => l.Timestamp).Take(maxDisplayCount);
            var firstLog = failuresToDisplay.First();
            var title = string.IsNullOrWhiteSpace(firstLog.AppId)
                ? $"*{firstLog.Source}*\n"
                : $"*{firstLog.AppId}* ({firstLog.Source})\n";

            var sb = new StringBuilder(title);
            var countText = (errors.Length > maxDisplayCount)
                ? $"{maxDisplayCount}+"
                : $"{errors.Length}";
            sb.AppendLine($"{countText} error(s) found over the past `{settings.SourcePreviousSpan.TotalMinutes}` minutes");
            foreach (var logEntry in failuresToDisplay)
            {
                var details = logEntry.Message.Length > 1024 ? logEntry.Message.Substring(0, 1024) : logEntry.Message;
                sb.AppendLine($"*{logEntry.Timestamp}* {logEntry.InstanceId} {logEntry.Level}");
                sb.AppendLine($"{details}");
                sb.AppendLine();
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
                            nameof(ErrorLogsSlackSink), destination, ex.ToString(), (int)timeSpan.TotalSeconds, retryCount);
                    });
        }
    }
}