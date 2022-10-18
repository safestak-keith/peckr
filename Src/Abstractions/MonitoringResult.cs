namespace Peckr.Abstractions
{
    public enum MonitoringOutcome
    {
        Polling,
        Success,
        Failure,
        TimedOut
    }

    public class MonitoringResult<T>
    {
        private static readonly MonitoringResult<T> PollingInstance = new MonitoringResult<T>(outcome: MonitoringOutcome.Polling);
        private static readonly MonitoringResult<T> SuccessInstance = new MonitoringResult<T>(outcome: MonitoringOutcome.Success);
        private static readonly MonitoringResult<T> FailureInstance = new MonitoringResult<T>(outcome: MonitoringOutcome.Failure);
        private static readonly MonitoringResult<T> TimedOutInstance = new MonitoringResult<T>(outcome: MonitoringOutcome.TimedOut);

        public MonitoringOutcome Outcome { get; }

        public T Result { get; }

        public MonitoringResult(MonitoringOutcome outcome, T result = default)
        {
            Outcome = outcome;
            Result = result;
        }

        public static MonitoringResult<T> Polling(T result) =>
            result == null ? PollingInstance : new MonitoringResult<T>(MonitoringOutcome.Polling, result);

        public static MonitoringResult<T> Success(T result) => 
            result == null ? SuccessInstance : new MonitoringResult<T>(MonitoringOutcome.Success, result);

        public static MonitoringResult<T> Failure(T result) => 
            result == null ? FailureInstance : new MonitoringResult<T>(MonitoringOutcome.Failure, result);

        public static MonitoringResult<T> TimedOut(T result) =>
            result == null ? TimedOutInstance : new MonitoringResult<T>(MonitoringOutcome.TimedOut, result);
    }
}