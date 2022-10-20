using System.Threading;
using System.Threading.Tasks;
using Peckr.Abstractions;

namespace Peckr.ConsoleApp
{
    public interface IConsolePeckr
    {
        Task<ConsoleExitCode> PeckAsync(
            PeckrSettings settings, CancellationTokenSource cts);
    }

    public class PollingConsolePeckr<T> : IConsolePeckr
    {
        private readonly IPeckResultPoller<T> _poller;
        private readonly IPeckResultSink<T> _resultSink;

        public PollingConsolePeckr(
            IPeckResultPoller<T> poller,
            IPeckResultSink<T> monitoringResultSink)
        {
            _poller = poller;
            _resultSink = monitoringResultSink;
        }

        public async Task<ConsoleExitCode> PeckAsync(
            PeckrSettings settings, CancellationTokenSource cts)
        {
            await foreach (var peckResult in _poller.PollAsync(settings, cts.Token).ConfigureAwait(false))
            {
                await _resultSink.PushMonitoringResultAsync(peckResult, settings, cts.Token).ConfigureAwait(false);
                switch (peckResult.Outcome)
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