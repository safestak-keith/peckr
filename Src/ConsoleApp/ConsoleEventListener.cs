using System;
using System.Diagnostics.Tracing;
using System.Linq;

namespace DiagnosticsMonitor.ConsoleApp
{
    public sealed class ConsoleEventListener : EventListener
    {
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (IsEventCounter(eventData)) 
                return;
            var eventMessage = string.Format(eventData.Message, eventData.Payload?.ToArray() ?? Array.Empty<object>());
            var message = $"{DateTime.Now:dd-MM-yyyy HH:mm:ss} {eventData.EventId} {eventData.Level} {eventMessage}";
            if (eventData.Level <= EventLevel.Error)
                Console.Error.WriteLine(message);
            else
                Console.WriteLine(message);
        }

        private static bool IsEventCounter(EventWrittenEventArgs eventData)
        {
            return eventData.EventName == "EventCounters";
        }
    }
}
