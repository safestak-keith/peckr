using System.Threading;
using System.Threading.Tasks;

namespace Peckr.Abstractions
{
    public class VoidMonitoringResultSink<T> : IMonitoringResultSink<T>
    {
        public static readonly VoidMonitoringResultSink<T> Instance = new VoidMonitoringResultSink<T>();

        private VoidMonitoringResultSink() { }

        public ValueTask PushMonitoringResultAsync(
            MonitoringResult<T> result, MonitorSettings settings, CancellationToken ct)
        {
            return default;
        }
    }
}