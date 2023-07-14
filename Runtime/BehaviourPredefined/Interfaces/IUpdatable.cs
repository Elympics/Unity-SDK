namespace Elympics
{
    public interface IUpdatable : IObservable
    {
        /// <summary>
        /// Predictable equivalent of FixedUpdate. It should contain game logic and perform operations depending on synchronized variables and predictable behaviours.
        /// </summary>
        void ElympicsUpdate();
    }
}
