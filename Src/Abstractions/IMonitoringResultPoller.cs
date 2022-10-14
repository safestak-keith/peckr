using System.Collections.Generic;
using System.Threading;

namespace DiagnosticsMonitor.Abstractions
{   
    public interface IMonitoringResultPoller<T>
    {
        IAsyncEnumerable<MonitoringResult<T>> PollAsync(
            MonitorSettings settings,
            [EnumeratorCancellation] CancellationToken ct);
    }
}