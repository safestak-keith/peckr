using System.Collections.Generic;
using System.Runtime.CompilerServices;
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