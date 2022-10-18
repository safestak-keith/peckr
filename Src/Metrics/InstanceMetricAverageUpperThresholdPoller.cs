using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticsMonitor.Abstractions;
using static DiagnosticsMonitor.Abstractions.PeckrEventSource;

namespace DiagnosticsMonitor.Metrics
{
    public class InstanceMetricAverageUpperThresholdPoller : IMonitoringResultPoller<IReadOnlyCollection<Metric>>
    {
        private readonly IMonitorDataRetriever<IReadOnlyCollection<Metric>> _metricRetriever;

        public InstanceMetricAverageUpperThresholdPoller(
            IMonitorDataRetriever<IReadOnlyCollection<Metric>> metricRetriever)
        {
            _metricRetriever = metricRetriever;
        }

        public async IAsyncEnumerable<MonitoringResult<IReadOnlyCollection<Metric>>> PollAsync(
            MonitorSettings settings,
            [EnumeratorCancellation]CancellationToken ct)
        {
            var sequence = 0;
            var runningStopwatch = Stopwatch.StartNew();

            while (ShouldKeepRunning(settings, runningStopwatch))
            {
                Log.PollerPolling(nameof(InstanceMetricAverageUpperThresholdPoller), sequence++);

                var allMetrics = await _metricRetriever.GetAsync(
                    DateTimeOffset.UtcNow.Subtract(settings.SourcePreviousSpan),
                    DateTimeOffset.UtcNow,
                    settings.SourceTakeLimit,
                    settings.SourceFilter,
                    allowFailures: true,// TODO: add this flag to config/settings
                    ct).ConfigureAwait(false);

                var instanceAverageMetrics = allMetrics.AggregateInstanceAverageMetrics(
                    settings.SourceAppOrResourceId,
                    DateTimeOffset.UtcNow.Subtract(settings.SourcePreviousSpan));

                var hasFailed = instanceAverageMetrics.Any(m => m.Value > settings.PrimaryThresholdValue);
                if (hasFailed)
                {
                    yield return MonitoringResult<IReadOnlyCollection<Metric>>.Failure(instanceAverageMetrics);
                    if (settings.TerminateWhenConditionMet)
                    {
                        Log.PollerTerminated(nameof(InstanceMetricAverageUpperThresholdPoller));
                        yield break;
                    }

                    await Task.Delay(settings.CooldownPeriodWhenConditionMet, ct).ConfigureAwait(false);
                }
                else
                {
                    yield return MonitoringResult<IReadOnlyCollection<Metric>>.Polling(instanceAverageMetrics);
                }

                await Task.Delay(settings.PollingDelay, ct).ConfigureAwait(false);
            }
            Log.PollerCompleted(nameof(InstanceMetricAverageUpperThresholdPoller));

            static bool ShouldKeepRunning(MonitorSettings ms, Stopwatch rs)
            {
                return ms.ExpectedRunDuration == TimeSpan.Zero
                    || rs.ElapsedMilliseconds < ms.ExpectedRunDuration.TotalMilliseconds;
            }
        }
    }
}