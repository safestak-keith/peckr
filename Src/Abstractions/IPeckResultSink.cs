using System.Threading;
using System.Threading.Tasks;

namespace Peckr.Abstractions
{
    public interface IPeckResultSink<T>
    {
        ValueTask PushMonitoringResultAsync(
            PeckResult<T> result,
            PeckrSettings settings,
            CancellationToken ct);
    }
}