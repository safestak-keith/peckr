using System;

namespace Peckr.Logs
{
    public enum LogLevel
    {
        Debug = 4,
        Information = 3,
        Error = 2,
        Fatal = 1,
    }

    public class LogEntry : IEquatable<LogEntry>
    {
        public string Source { get; }
        public string AppId { get; }
        public DateTimeOffset Timestamp { get; }
        public LogLevel Level { get; }
        public string InstanceId { get; }
        public int EventId { get; }
        public string OperationId { get; }
        public string Message { get; }

        public LogEntry(
            string source, 
            string appId, 
            DateTimeOffset timestamp, 
            LogLevel level, 
            string instanceId, 
            int eventId, 
            string operationId, 
            string message)
        {
            Source = source;
            AppId = appId;
            Timestamp = timestamp;
            Level = level;
            InstanceId = instanceId;
            EventId = eventId;
            OperationId = operationId;
            Message = message;
        }

        public bool Equals(LogEntry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Source, other.Source) && string.Equals(AppId, other.AppId) && Timestamp.Equals(other.Timestamp) && Level == other.Level && string.Equals(InstanceId, other.InstanceId) && EventId == other.EventId && string.Equals(OperationId, other.OperationId) && string.Equals(Message, other.Message);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LogEntry)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Source != null ? Source.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (AppId != null ? AppId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Timestamp.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Level;
                hashCode = (hashCode * 397) ^ (InstanceId != null ? InstanceId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ EventId;
                hashCode = (hashCode * 397) ^ (OperationId != null ? OperationId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Message != null ? Message.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
