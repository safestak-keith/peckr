using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Peckr.Abstractions;
using static Peckr.Abstractions.PeckrEventSource;

namespace Peckr.Metrics
{
    public class MetricsPoller: IPeckResultPoller<IReadOnlyCollection<Metric>>
    {
        private readonly IPeckDataRetriever<IReadOnlyCollection<Metric>> _metricRetriever;
        private readonly IPeckResultEvaluator<IReadOnlyCollection<Metric>> _metricEvaluator;
        private readonly bool _shouldEmitSuccessWhenConditionIsMet;

        public MetricsPoller(
            IPeckDataRetriever<IReadOnlyCollection<Metric>> metricRetriever,
            IPeckResultEvaluator<IReadOnlyCollection<Metric>> metricEvaluator,
            bool shouldEmitSuccessWhenConditionIsMet = true
        )
        {
            _metricRetriever = metricRetriever;
            _metricEvaluator = metricEvaluator;
            _shouldEmitSuccessWhenConditionIsMet = shouldEmitSuccessWhenConditionIsMet;
        }

        public async IAsyncEnumerable<PeckResult<IReadOnlyCollection<Metric>>> PollAsync(PeckrSettings settings, [EnumeratorCancellation] CancellationToken ct)
        {
            var stopWatch = Stopwatch.StartNew();
            var metrics = Array.Empty<Metric>();

            while (ShouldKeepRunning(settings, stopWatch))
            {
                metrics = (
                    await _metricRetriever.GetAsync(DateTimeOffset.UtcNow.Subtract(settings.SourcePreviousSpan),
                        DateTimeOffset.UtcNow, settings.SourceTakeLimit, settings.SourceFilter, true, ct)
                ).ToArray();

                if (_metricEvaluator.IsConditionMet(metrics))
                {
                    yield return _shouldEmitSuccessWhenConditionIsMet
                        ? PeckResult<IReadOnlyCollection<Metric>>.Success(metrics)
                        : PeckResult<IReadOnlyCollection<Metric>>.Failure(metrics);

                    if (settings.TerminateWhenConditionMet)
                    {
                        Log.PollerTerminated(nameof(MetricsPoller));
                        yield break;
                    }

                    await Task.Delay(settings.CooldownPeriodWhenConditionMet, ct).ConfigureAwait(false);
                }
                else
                {
                    yield return PeckResult<IReadOnlyCollection<Metric>>.Polling(metrics);
                }

                await Task.Delay(settings.PollingDelay, ct).ConfigureAwait(false);
            }

            if (settings.ShouldFailOnRunDurationExceeded)
            {
                Log.PollerTimedout(nameof(MetricsPoller));
                yield return PeckResult<IReadOnlyCollection<Metric>>.TimedOut(metrics);
            }

            Log.PollerCompleted(nameof(MetricsPoller));
        }
        
        private static bool ShouldKeepRunning(PeckrSettings ms, Stopwatch rs)
        {
            return ms.ExpectedRunDuration == TimeSpan.Zero
                   || rs.ElapsedMilliseconds < ms.ExpectedRunDuration.TotalMilliseconds;
        }
    }
}