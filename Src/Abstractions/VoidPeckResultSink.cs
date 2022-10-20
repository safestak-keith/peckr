using System.Threading;
using System.Threading.Tasks;

namespace Peckr.Abstractions
{
    public class VoidPeckResultSink<T> : IPeckResultSink<T>
    {
        public static readonly VoidPeckResultSink<T> Instance = new VoidPeckResultSink<T>();

        private VoidPeckResultSink() { }

        public ValueTask PushMonitoringResultAsync(
            PeckResult<T> result, PeckrSettings settings, CancellationToken ct)
        {
            return default;
        }
    }
}