using Elympics.ElympicsSystems.Internal;

namespace Elympics
{
    internal abstract class GameSceneInitializer
    {
        public abstract void Initialize(
            ElympicsClient client,
            ElympicsBot bot,
            ElympicsServer server,
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager,
            ElympicsLoggerContext logger);

        public virtual void Dispose()
        { }
    }
}
