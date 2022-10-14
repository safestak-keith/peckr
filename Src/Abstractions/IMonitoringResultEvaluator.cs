namespace DiagnosticsMonitor.Abstractions
{
    public interface IMonitoringResultEvaluator<in TResult>
    {
        public bool IsConditionMet(TResult result);
    }
}
