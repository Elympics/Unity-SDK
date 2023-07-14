namespace Elympics
{
    public interface IInitializable : IObservable
    {
        /// <summary>
        /// Called during ElympicsBehaviour initialization just before gathering all its <see cref="ElympicsVar"/>s for enabling their synchronization.
        /// </summary>
        void Initialize();
    }
}
