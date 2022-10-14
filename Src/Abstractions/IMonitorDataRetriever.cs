using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosticsMonitor.Abstractions
{
    public interface IMonitorDataRetriever<TResult>
    {
        Task<TResult> GetAsync(
            DateTimeOffset start,
            DateTimeOffset end,
            int takeLimit, 
            string customFilter = null, 
            bool allowFailures = true,
            CancellationToken ct = default);
    }
}