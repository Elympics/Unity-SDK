namespace Elympics
{
    public interface IReconciliationHandler : IObservable
    {
        /// <summary>
        /// Called just before reconciliation.
        /// </summary>
        void OnPreReconcile();

        /// <summary>
        /// Called after reconciliation.
        /// </summary>
        void OnPostReconcile();
    }
}
