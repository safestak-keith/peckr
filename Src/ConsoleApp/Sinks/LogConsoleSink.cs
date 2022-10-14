using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticsMonitor.Abstractions;
using DiagnosticsMonitor.Logs;

namespace DiagnosticsMonitor.ConsoleApp.Sinks
{
    public class LogConsoleSink : IMonitoringResultSink<IReadOnlyCollection<LogEntry>>
    {
        private readonly IMonitoringResultSink<IReadOnlyCollection<LogEntry>> _decoratedSink;

        public LogConsoleSink(IMonitoringResultSink<IReadOnlyCollection<LogEntry>> decoratedSink)
        {
            _decoratedSink = decoratedSink;
        }

        public async ValueTask PushMonitoringResultAsync(
            MonitoringResult<IReadOnlyCollection<LogEntry>> monitoringResult,
            MonitorSettings settings, 
            CancellationToken ct)
        {
            if (monitoringResult.IsFailure(settings))
            {
                OutputLogResults(monitoringResult.Result, Console.Error.WriteLine);
            }
            else
            {
                OutputLogResults(monitoringResult.Result, Console.WriteLine);
            }

            await _decoratedSink.PushMonitoringResultAsync(monitoringResult, settings, ct).ConfigureAwait(false);
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