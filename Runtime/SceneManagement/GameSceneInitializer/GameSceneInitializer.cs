namespace Elympics
{
    internal abstract class GameSceneInitializer
    {
        public abstract void Initialize(
            ElympicsClient client,
            ElympicsBot bot,
            ElympicsServer server,
            ElympicsSinglePlayer singlePlayer,
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager);

        public virtual void Dispose()
        {
        }
    }
}
