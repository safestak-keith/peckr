namespace DiagnosticsMonitor.Abstractions
{
    public interface IDataProcessor<in TResultIn, out TResultOut>
    {
        TResultOut Process(TResultIn result);
    }
}
