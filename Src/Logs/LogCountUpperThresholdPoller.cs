﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Peckr.Abstractions;
using static Peckr.Abstractions.PeckrEventSource;

namespace Peckr.Logs
{
    public class LogCountUpperThresholdPoller : IPeckResultPoller<IReadOnlyCollection<LogEntry>>
    {
        private readonly IPeckDataRetriever<IReadOnlyCollection<LogEntry>> _logRetriever;

        public LogCountUpperThresholdPoller(
            IPeckDataRetriever<IReadOnlyCollection<LogEntry>> logRetriever)
        {
            _logRetriever = logRetriever;
        }

        public async IAsyncEnumerable<PeckResult<IReadOnlyCollection<LogEntry>>> PollAsync(
            PeckrSettings settings,
            [EnumeratorCancellation]CancellationToken ct)
        {
            var sequence = 0;
            var runningStopwatch = Stopwatch.StartNew();

            while (ShouldKeepRunning(settings, runningStopwatch))
            {
                Log.PollerPolling(nameof(LogCountUpperThresholdPoller), sequence++);
                var logs = await _logRetriever.GetAsync(
                    DateTimeOffset.UtcNow.Subtract(settings.SourcePreviousSpan),
                    DateTimeOffset.UtcNow,
                    settings.SourceTakeLimit, 
                    settings.SourceFilter, 
                    allowFailures: true, // TODO: add this flag to config/settings
                    ct).ConfigureAwait(false);

                var hasFailed = logs.Count > settings.PrimaryThresholdValue;
                if (hasFailed)
                {
                    yield return PeckResult<IReadOnlyCollection<LogEntry>>.Failure(logs);
                    if (settings.TerminateWhenConditionMet)
                    {
                        Log.PollerTerminated(nameof(LogCountUpperThresholdPoller));
                        yield break;
                    }

                    await Task.Delay(settings.CooldownPeriodWhenConditionMet, ct).ConfigureAwait(false);
                }
                else
                {
                    yield return PeckResult<IReadOnlyCollection<LogEntry>>.Polling(logs);
                }

                await Task.Delay(settings.PollingDelay, ct).ConfigureAwait(false);
            }
            Log.PollerCompleted(nameof(LogCountUpperThresholdPoller));

            static bool ShouldKeepRunning(PeckrSettings ms, Stopwatch rs)
            {
                return ms.ExpectedRunDuration == TimeSpan.Zero
                    || rs.ElapsedMilliseconds < ms.ExpectedRunDuration.TotalMilliseconds;
            };
        }
    }
}