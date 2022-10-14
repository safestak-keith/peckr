using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticsMonitor.Abstractions;
using DiagnosticsMonitor.Metrics;

namespace DiagnosticsMonitor.ConsoleApp.Sinks
{
    public class MetricConsoleSink : IMonitoringResultSink<IReadOnlyCollection<Metric>>
    {
        private readonly IMonitoringResultSink<IReadOnlyCollection<Metric>> _decoratedSink;

        public MetricConsoleSink(IMonitoringResultSink<IReadOnlyCollection<Metric>> decoratedSink)
        {
            _decoratedSink = decoratedSink;
        }

        public async ValueTask PushMonitoringResultAsync(
            MonitoringResult<IReadOnlyCollection<Metric>> monitoringResult,
            MonitorSettings settings,
            CancellationToken ct)
        {
            if (monitoringResult.IsFailure(settings))
            {
                OutputMetricResults(monitoringResult.Result, Console.Error.WriteLine);
            }
            else
            {
                OutputMetricResults(monitoringResult.Result, Console.WriteLine);
            }

            await _decoratedSink.PushMonitoringResultAsync(monitoringResult, settings, ct).ConfigureAwait(false);
        }

        private static void OutputMetricResults(IReadOnlyCollection<Metric> results, Action<string> printLine)
        {
            if (!results.Any())
                return;

            var sb = new StringBuilder($"Found {results.Count} metrics{Environment.NewLine}");

            foreach (var result in results)
            {
                sb.AppendLine($"Timestamp: { result.Timestamp}\nSource: {result.Source}\nApp: {result.AppId}\nType: {result.Type}\nName: {result.Name}\nTarget: {result.Target}\nInstance: {result.InstanceId}, Value: {result.Value:N}\n");
                sb.AppendLine("------------------------------------------------------------\n");
                if (result.Trace != null)
                {
                    sb.AppendLine($"Trace:\n{result.Trace}\n");
                    sb.AppendLine("------------------------------------------------------------\n");
                }
            }

            printLine(sb.ToString());
        }
    }
}