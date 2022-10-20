using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Peckr.Abstractions;
using Peckr.Logs;

namespace Peckr.ConsoleApp.Sinks
{
    public class LogConsoleSink : IPeckResultSink<IReadOnlyCollection<LogEntry>>
    {
        private readonly IPeckResultSink<IReadOnlyCollection<LogEntry>> _decoratedSink;

        public LogConsoleSink(IPeckResultSink<IReadOnlyCollection<LogEntry>> decoratedSink)
        {
            _decoratedSink = decoratedSink;
        }

        public async ValueTask PushMonitoringResultAsync(
            PeckResult<IReadOnlyCollection<LogEntry>> peckResult,
            PeckrSettings settings, 
            CancellationToken ct)
        {
            if (peckResult.IsFailure(settings))
            {
                OutputLogResults(peckResult.Result, Console.Error.WriteLine);
            }
            else
            {
                OutputLogResults(peckResult.Result, Console.WriteLine);
            }

            await _decoratedSink.PushMonitoringResultAsync(peckResult, settings, ct).ConfigureAwait(false);
        }

        private static void OutputLogResults(IReadOnlyCollection<LogEntry> results, Action<string> printLine)
        {
            var sb = new StringBuilder($"Found {results.Count} log entries{Environment.NewLine}");
            foreach (var result in results)
            {
                sb.AppendLine($"Source: {result.Source}\nApp: {result.AppId}\nLevel: {result.Level}\nEventId: {result.EventId}\nTimestamp: {result.Timestamp}\nMessage: {result.Message}");
                sb.AppendLine("------------------------------------------------------------");
            }

            printLine(sb.ToString());
        }
    }
}