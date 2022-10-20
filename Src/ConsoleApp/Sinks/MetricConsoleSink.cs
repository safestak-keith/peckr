using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Peckr.Abstractions;
using Peckr.Metrics;

namespace Peckr.ConsoleApp.Sinks
{
    public class MetricConsoleSink : IPeckResultSink<IReadOnlyCollection<Metric>>
    {
        private readonly IPeckResultSink<IReadOnlyCollection<Metric>> _decoratedSink;

        public MetricConsoleSink(IPeckResultSink<IReadOnlyCollection<Metric>> decoratedSink)
        {
            _decoratedSink = decoratedSink;
        }

        public async ValueTask PushMonitoringResultAsync(
            PeckResult<IReadOnlyCollection<Metric>> peckResult,
            PeckrSettings settings,
            CancellationToken ct)
        {
            if (peckResult.IsFailure(settings))
            {
                OutputMetricResults(peckResult.Result, Console.Error.WriteLine);
            }
            else
            {
                OutputMetricResults(peckResult.Result, Console.WriteLine);
            }

            await _decoratedSink.PushMonitoringResultAsync(peckResult, settings, ct).ConfigureAwait(false);
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