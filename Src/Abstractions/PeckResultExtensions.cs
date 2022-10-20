using System.Collections.Generic;

namespace Peckr.Abstractions
{
    public static class PeckResultExtensions
    {
        public static bool IsFailure<T>(this PeckResult<IReadOnlyCollection<T>> result, PeckrSettings settings)
            => result.Outcome == MonitoringOutcome.Failure ||
               result.Outcome == MonitoringOutcome.TimedOut && settings.ShouldFailOnRunDurationExceeded;
    }
}
