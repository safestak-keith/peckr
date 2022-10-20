using System;
using System.Diagnostics.Tracing;

namespace Peckr.Abstractions
{
    [EventSource(Name = "DiagnosticsMonitor-EventSource")]
    public sealed class PeckrEventSource : EventSource
    {
        public static readonly PeckrEventSource Log = new PeckrEventSource();

        private readonly EventCounter _programElapsedCounter;
        private readonly EventCounter _retrievalElapsedCounter;
        private readonly EventCounter _sinkPushElapsedCounter;

        private PeckrEventSource()
        {
            _programElapsedCounter = new EventCounter("ProgramCompletedElapsed", this);
            _retrievalElapsedCounter = new EventCounter("RetrieverRetrievalElapsed", this);
            _sinkPushElapsedCounter = new EventCounter("SinkPushElapsed", this);
        }

        [Event(
            EventIds.ProgramUnhandledException,
            Message = "Diagnostics Monitor ended with unhandled exception {0}",
            Level = EventLevel.Error,
            Keywords = Keywords.Program)]
        public void ProgramUnhandledException(string exception)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.ProgramUnhandledException, exception);
            }
        }

        [Event(
            EventIds.ProgramConfigurationError,
            Message = "Diagnostics Monitor received bad configuration: {0}",
            Level = EventLevel.Error,
            Keywords = Keywords.Program)]
        public void ProgramConfigurationError(string configurationErrors)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.ProgramConfigurationError, configurationErrors);
            }
        }

        [Event(
            EventIds.ProgramCancelled,
            Message = "Diagnostics Monitor cancelled",
            Level = EventLevel.Warning,
            Keywords = Keywords.Program)]
        public void ProgramCancelled()
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.ProgramCancelled);
            }
        }

        [Event(
            EventIds.ProgramStarted,
            Message = "Diagnostics Monitor started for type {0} from {1} and pushing to sink type {2}",
            Level = EventLevel.Informational,
            Keywords = Keywords.Program)]
        public void ProgramStarted(string monitorType, string source, string sinkType)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.ProgramStarted, monitorType, source, sinkType);
            }
        }

        [Event(
            EventIds.ProgramCompleted,
            Message = "Diagnostics Monitor completed after {0}",
            Level = EventLevel.Informational,
            Keywords = Keywords.Program)]
        public void ProgramCompleted(long elapsedMilliseconds)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.ProgramCompleted, TimeSpan.FromMilliseconds(elapsedMilliseconds));
                _programElapsedCounter.WriteMetric(elapsedMilliseconds);
            }
        }

        [Event(
            EventIds.PollerPolling,
            Message = "Poller {0} polling (sequence {1})",
            Level = EventLevel.Informational,
            Keywords = Keywords.Poller)]
        public void PollerPolling(string name, int sequence)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.PollerPolling, name, sequence);
            }
        }

        [Event(
            EventIds.PollerCompleted,
            Message = "Poller {0} completed",
            Level = EventLevel.Informational,
            Keywords = Keywords.Poller)]
        public void PollerCompleted(string name)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.PollerCompleted, name);
            }
        }

        [Event(
            EventIds.PollerTerminated,
            Message = "Poller {0} terminated",
            Level = EventLevel.Informational,
            Keywords = Keywords.Poller)]
        public void PollerTerminated(string name)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.PollerTerminated, name);
            }
        }

        [Event(
            EventIds.PollerTimedout,
            Message = "Poller {0} timed-out",
            Level = EventLevel.Informational,
            Keywords = Keywords.Poller)]
        public void PollerTimedout(string name)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.PollerTimedout, name);
            }
        }

        [Event(
            EventIds.RetrieverUnhandledException,
            Message = "Retriever {0} encountered unhandled exception {1}",
            Level = EventLevel.Error,
            Keywords = Keywords.Retriever)]
        public void RetrieverUnhandledException(string name, string exception)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.RetrieverUnhandledException, name, exception);
            }
        }

        [Event(
            EventIds.RetrieverRetrievalCompleted, 
            Message = "Retriever {0} retrieval completed from {1}, found {2} items from {3} to {4} and took {5}ms",
            Level = EventLevel.Informational, 
            Keywords = Keywords.Retriever)]
        public void RetrieverRetrievalCompleted(
            string name, string source, int count, string start, string end, long elapsedMilliseconds)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.RetrieverRetrievalCompleted, name, source, count, start, end, elapsedMilliseconds);
                _retrievalElapsedCounter.WriteMetric(elapsedMilliseconds);
            }
        }

        [Event(
            EventIds.RetrieverRetrievalNoResults,
            Message = "Retriever {0} retrieval completed from {1}, no results found",
            Level = EventLevel.Informational,
            Keywords = Keywords.Retriever)]
        public void RetrieverRetrievalNoResults(string name, string source)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.RetrieverRetrievalNoResults, name, source);
            }
        }

        [Event(
            EventIds.RetrieverRetrievalRetry,
            Message = "Retriever {0} retrieval retry attempt to {1}, due to {2} waiting {3}s after {4} attempts",
            Level = EventLevel.Informational,
            Keywords = Keywords.Retriever)]
        public void RetrieverRetrievalRetry(
            string name, string source, string reason, int retryWaitSeconds, int retryCount)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.RetrieverRetrievalRetry, name, source, reason, retryWaitSeconds, retryCount);
            }
        }

        [Event(
            EventIds.SinkUnhandledException,
            Message = "Sink {0} encountered unhandled exception {1}",
            Level = EventLevel.Error,
            Keywords = Keywords.Sink)]
        public void SinkUnhandledException(string sinkType, string exception)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.SinkUnhandledException, sinkType, exception);
            }
        }

        [Event(
            EventIds.SinkPushCompleted,
            Message = "Sink {0} completed push to {1} with {2} items and took {3}ms",
            Level = EventLevel.Informational,
            Keywords = Keywords.Sink)]
        public void SinkPushCompleted(
            string sinkType, string destination, int count, long elapsedMilliseconds)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.SinkPushCompleted, sinkType, destination, count, elapsedMilliseconds);
                _sinkPushElapsedCounter.WriteMetric(elapsedMilliseconds);
            }
        }

        [Event(
            EventIds.SinkPushRetry,
            Message = "Sink {0} retrieval retry attempt to {1}, due to {2} waiting {3}s after {4} attempts",
            Level = EventLevel.Informational,
            Keywords = Keywords.Sink)]
        public void SinkPushRetry(
            string name, string source, string reason, int retryWaitSeconds, int retryCount)
        {
            if (IsEnabled())
            {
                WriteEvent(EventIds.SinkPushRetry, name, source, reason, retryWaitSeconds, retryCount);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _programElapsedCounter.Dispose();
            _retrievalElapsedCounter.Dispose();
            _sinkPushElapsedCounter.Dispose();
        }

        #region Fixed Event Definitions
        public static class EventIds
        {
            public const int ProgramUnhandledException = 1_000;
            public const int ProgramConfigurationError = 1_001;
            public const int ProgramCancelled = 1_002;
            public const int ProgramStarted = 1_100;
            public const int ProgramCompleted = 1_101;
            public const int PollerPolling = 4_100;
            public const int PollerCompleted = 4_101;
            public const int PollerTerminated = 4_102;
            public const int PollerTimedout = 4_103;
            public const int RetrieverUnhandledException = 8_000;
            public const int RetrieverRetrievalTimeout = 8_001;
            public const int RetrieverRetrievalRetry = 8_002;
            public const int RetrieverRetrievalCompleted = 8_101;
            public const int RetrieverRetrievalNoResults = 8_102;
            public const int SinkUnhandledException = 16_000;
            public const int SinkPushTimeout = 16_001;
            public const int SinkPushRetry = 16_002;
            public const int SinkPushCompleted = 16_101;
        }

        public static class Keywords
        {
            public const EventKeywords Program = (EventKeywords)1;
            public const EventKeywords Monitor = (EventKeywords)2;
            public const EventKeywords Poller = (EventKeywords)4;
            public const EventKeywords Retriever = (EventKeywords)8;
            public const EventKeywords Sink = (EventKeywords)16;
        }
        #endregion
    }
}