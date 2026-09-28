namespace Elympics
{
    internal abstract class GameSceneInitializer
    {
        public abstract void Initialize(
            ElympicsClient client,
            ElympicsServer server,
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager);

        /// <summary>
        /// True if the match reports being joined through <see cref="ElympicsBehavioursManager"/> (clients and single player).
        /// False for server-side modes, which count as started once initialized.
        /// </summary>
        public virtual bool ReportsMatchJoined => true;

        public virtual void Dispose()
        { }
    }
}
