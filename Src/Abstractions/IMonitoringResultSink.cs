using System.Threading;
using System.Threading.Tasks;

namespace DiagnosticsMonitor.Abstractions
{
    public interface IMonitoringResultSink<T>
    {
        ValueTask PushMonitoringResultAsync(
            MonitoringResult<T> result,
            MonitorSettings settings,
            CancellationToken ct);
    }
}