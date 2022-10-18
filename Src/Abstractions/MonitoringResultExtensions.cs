using System.Collections.Generic;

namespace Peckr.Abstractions
{
    public static class MonitoringResultExtensions
    {
        public static bool IsFailure<T>(this MonitoringResult<IReadOnlyCollection<T>> result, MonitorSettings settings)
            => result.Outcome == MonitoringOutcome.Failure ||
               result.Outcome == MonitoringOutcome.TimedOut && settings.ShouldFailOnRunDurationExceeded;
    }
}
