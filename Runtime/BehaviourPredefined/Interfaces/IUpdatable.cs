using Elympics.Public;
namespace Elympics
{
    public interface IUpdatable : IObservable
    {
        /// <summary>
        /// Predictable equivalent of FixedUpdate. It should contain game logic and perform operations depending on synchronized variables and predictable behaviours.
        /// </summary>
        void ElympicsUpdate();

        /// <summary>
        /// Called when prediction is blocked or unblocked for this behaviour (e.g., due to lag).
        /// Only called when prediction is enabled in Elympics configuration.
        /// </summary>
        void PredictionStatusChanged(bool isBlocked, NetworkCondition networkCondition)
        { }
    }
}
