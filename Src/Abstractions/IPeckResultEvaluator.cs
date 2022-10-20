namespace Peckr.Abstractions
{
    public interface IPeckResultEvaluator<in TResult>
    {
        public bool IsConditionMet(TResult result);
    }
}
