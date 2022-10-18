using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticsMonitor.Abstractions;
using Microsoft.Extensions.Configuration;
using static DiagnosticsMonitor.Abstractions.PeckrEventSource;

namespace DiagnosticsMonitor.ConsoleApp
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            if (args.Length == 0 || args.Any(a => a == "help" || a == "-h" || a == "--help"))
            {
                PrintHelp();
                return (int)ConsoleExitCode.Success;
            }

            CreateAndEnableConsoleEventListener();
            using var cts = SetupUserInputCancellationTokenSource();

            var settings = TryDeriveSettings(args);
            if (settings == null)
            {
                return (int)ConsoleExitCode.UnknownError;
            }

            Log.ProgramStarted(settings.MonitorType.ToString(), settings.SourceAppOrResourceId, settings.SinkType.ToString());
            var monitor = MonitorFactory.CreateMonitor(settings);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return (int)await monitor.MonitorAsync(settings, cts);
            }
            catch (OperationCanceledException)
            {
                Log.ProgramCancelled();
            }
            catch (Exception ex)
            {
                Log.ProgramUnhandledException(ex.ToString());
                return (int)ConsoleExitCode.UnknownError;
            }
            finally
            {
                Log.ProgramCompleted(stopwatch.ElapsedMilliseconds);
            }

            return (int)ConsoleExitCode.Success;
        }

        private static MonitorSettings TryDeriveSettings(string[] args)
        {
            try
            {
                return new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .AddCommandLine(args, ConsoleArgs.Mappings)
                    .Build()
                    .Get<MonitorConfiguration>()
                    .ToMonitorSettings();
            }
            catch (ArgumentException ex)
            {
                Log.ProgramConfigurationError(ex.Message);
                return null;
            }
        }

        private static void PrintHelp()
        {
            // Taken from README.md at root
            const string helpText = @"
USAGE: 
    
peckr [--help] --monitorType <type> --sourceConnection <connection> [--durationToRunMinutes <minutes> (default: 10)] [--pollingIntervalMilliseconds <millis> (default: 5000)] [--sourceFilter <filter>] [--sourcePreviousSpanMinutes <minutes> (default: 5)] [--sourceTakeLimit <count> (default: 10)] [--sourceAppOrResourceId <appId>] [--primaryThresholdValue <value> (default: 0)] [--secondaryThresholdValue <value> (default: 0)] [--terminateWhenConditionMet <bool> (default: true)] [--shouldFailOnRunDurationExceeded <bool> (default: false)] [--cooldownWhenConditionMetMinutes <c> (default: 5)] [--sinkType <type>] [--sinkConnection <connection>]

OPTIONS:

    --help or -h                                        display this list of options
    --durationToRunMinutes or -d <minutes>              duration in minutes to run the monitoring
    --pollingIntervalMilliseconds or -i <millis>        interval in milliseconds to poll the source
    --monitorType or -m <type>                          type of monitor (see Monitor Types)
    --sourceConnection or -c <connection>               connection string or details for the source
    --sourceFilter or -f <filter>                       filter string to apply to the source
    --sourcePreviousSpanMinutes or -p <minutes>         previous span in minutes to check from the source
    --sourceTakeLimit or -l <count>                     max number of items to take from the source
    --sourceAppOrResourceId or -a <id>                  app or resource ID of the source 
    --primaryThresholdValue or -v <value>               primary threshold value to check in the condition
    --secondaryThresholdValue or -u <value>             secondary threshold value to check in the condition
    --terminateWhenConditionMet or -t <bool>            true/false flag to terminate on condition
    --shouldFailOnRunDurationExceeded or -e <bool>      true/false flag to fail on runtime duration exceeded
    --cooldownWhenConditionMetMinutes or -w <c>         cooldown in minutes when the condition is met 
    --sinkType or -n <type>                             type of outbound sink for the monitoring results
    --sinkConnection or -o <connection>                 connection string or details for the sink

EXAMPLES:

peckr -m azwdperf_instavg_upperbound -l 1200 -v 75 -c ""DefaultEndpointsProtocol=https;AccountName=fooapistor;AccountKey=xxxxxxxxxxxxxxxxxxxx=="" -f ""Counter eq 'Processor\% Processor Time'"" 

peckr --monitorType azwdlogs_errscnt_upperbound --sourceTakeLimit 100 --sourceConnection ""DefaultEndpointsProtocol=https;AccountName=fooapistor;AccountKey=xxxxxxxxxxxxxxxxxxxx=="" --sourceAppOrResourceId ""Foo API"" --sourceFilter ""EventId ne 1337"" --sinkType slackwebhook --sinkConnection ""https://hooks.slack.com/services/zzzzz/yyyyyy/xxxxxx""
";
            var versionString = (Assembly.GetEntryAssembly() ?? throw new InvalidOperationException())
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;

            Console.WriteLine($"Peckr v{versionString}");
            Console.WriteLine("=========================================");
            Console.WriteLine(helpText);
        }

        private static CancellationTokenSource SetupUserInputCancellationTokenSource()
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };
            return cts;
        }

        private static void CreateAndEnableConsoleEventListener()
        {
            var listener = new ConsoleEventListener();
            var args = new Dictionary<string, string>
            {
                {"EventCounterIntervalSec", "60"}
            };
            listener.EnableEvents(Log, EventLevel.Informational, EventKeywords.All, args);
        }
    }
}