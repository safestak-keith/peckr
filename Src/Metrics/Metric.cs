using System;

namespace Peckr.Metrics
{
    public enum AggregationType
    {
        None = 0,
        Min,
        Max,
        Sum,
        Count,
        Average,
        StDev,
        TDigest,
        Percentile50,
        Percentile90,
        Percentile95,
        Percentile99,
        Percentile999,
        Percentile9999,
        Percentile99999,
    }

    public class Metric : IEquatable<Metric>
    {
        public string Source { get; }
        public string AppId { get; }
        public DateTimeOffset Timestamp { get; }
        public string Type { get; }
        public string Name { get; }
        public string Target { get; }
        public double Value { get; }
        public string InstanceId { get; }
        public string OperationId { get; }
        public string Trace { get; set; }

        public Metric(
            string source,
            string appId,
            DateTimeOffset timestamp,
            string type,
            string name,
            string target,
            double value,
            string instanceId,
            string operationId = null)
        {
            Source = source;
            AppId = appId;
            Timestamp = timestamp;
            Type = type;
            Name = name;
            Target = target;
            Value = value;
            InstanceId = instanceId;
            OperationId = operationId;
        }

        public bool Equals(Metric other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Source, other.Source) && string.Equals(AppId, other.AppId) && Timestamp.Equals(other.Timestamp) && string.Equals(Type, other.Type) && string.Equals(Name, other.Name) && string.Equals(Target, other.Target) && Value == other.Value && string.Equals(InstanceId, other.InstanceId) && string.Equals(OperationId, other.OperationId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Metric) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Source != null ? Source.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (AppId != null ? AppId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Timestamp.GetHashCode();
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Target != null ? Target.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Value.GetHashCode();
                hashCode = (hashCode * 397) ^ (InstanceId != null ? InstanceId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (OperationId != null ? OperationId.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
