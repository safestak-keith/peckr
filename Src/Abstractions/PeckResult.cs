namespace Peckr.Abstractions
{
    public enum MonitoringOutcome
    {
        Polling,
        Success,
        Failure,
        TimedOut
    }

    public class PeckResult<T>
    {
        private static readonly PeckResult<T> PollingInstance = new PeckResult<T>(outcome: MonitoringOutcome.Polling);
        private static readonly PeckResult<T> SuccessInstance = new PeckResult<T>(outcome: MonitoringOutcome.Success);
        private static readonly PeckResult<T> FailureInstance = new PeckResult<T>(outcome: MonitoringOutcome.Failure);
        private static readonly PeckResult<T> TimedOutInstance = new PeckResult<T>(outcome: MonitoringOutcome.TimedOut);

        public MonitoringOutcome Outcome { get; }

        public T Result { get; }

        public PeckResult(MonitoringOutcome outcome, T result = default)
        {
            Outcome = outcome;
            Result = result;
        }

        public static PeckResult<T> Polling(T result) =>
            result == null ? PollingInstance : new PeckResult<T>(MonitoringOutcome.Polling, result);

        public static PeckResult<T> Success(T result) => 
            result == null ? SuccessInstance : new PeckResult<T>(MonitoringOutcome.Success, result);

        public static PeckResult<T> Failure(T result) => 
            result == null ? FailureInstance : new PeckResult<T>(MonitoringOutcome.Failure, result);

        public static PeckResult<T> TimedOut(T result) =>
            result == null ? TimedOutInstance : new PeckResult<T>(MonitoringOutcome.TimedOut, result);
    }
}