using System.Threading;
using System.Threading.Tasks;
using DiagnosticsMonitor.Abstractions;

namespace DiagnosticsMonitor.ConsoleApp
{
    public interface IConsoleMonitor
    {
        Task<ConsoleExitCode> MonitorAsync(
            MonitorSettings settings, CancellationTokenSource cts);
    }

    public class PollingConsoleMonitor<T> : IConsoleMonitor
    {
        private readonly IMonitoringResultPoller<T> _poller;
        private readonly IMonitoringResultSink<T> _monitoringResultSink;

        public PollingConsoleMonitor(
            IMonitoringResultPoller<T> poller,
            IMonitoringResultSink<T> monitoringResultSink)
        {
            _poller = poller;
            _monitoringResultSink = monitoringResultSink;
        }

        public async Task<ConsoleExitCode> MonitorAsync(
            MonitorSettings settings, CancellationTokenSource cts)
        {
            await foreach (var monitorResult in _poller.PollAsync(settings, cts.Token).ConfigureAwait(false))
            {
                await _monitoringResultSink.PushMonitoringResultAsync(monitorResult, settings, cts.Token).ConfigureAwait(false);
                switch (monitorResult.Outcome)
                {
                    case MonitoringOutcome.Polling:
                        break;
                    case MonitoringOutcome.Success:
                        if (settings.TerminateWhenConditionMet) 
                        {
                            return ConsoleExitCode.Success;
                        }   
                        break;
                    case MonitoringOutcome.Failure:
                        if (settings.TerminateWhenConditionMet)
                        {
                            return ConsoleExitCode.UnknownError;
                        }
                        break;
                    case MonitoringOutcome.TimedOut:
                        if (settings.ShouldFailOnRunDurationExceeded)
                        {
                            return ConsoleExitCode.TimedOut;
                        }
                        break;
                }
            }
            return ConsoleExitCode.Success;
        }
    }
}