namespace Elympics
{
    internal abstract class GameSceneInitializer
    {
        /// <summary>Set before <see cref="Initialize"/>. Null unless the scene was loaded by <see cref="ElympicsMatchLauncher"/>.</summary>
        public IElympicsMatchLifecycleSink LifecycleSink { get; set; }

        public abstract void Initialize(
            ElympicsClient client,
            ElympicsServer server,
            ElympicsGameConfig gameConfig,
            ElympicsBehavioursManager behavioursManager);

        /// <summary>False for server-side modes, which count as started once initialized instead of reporting joining through <see cref="ElympicsBehavioursManager"/>.</summary>
        public virtual bool ReportsMatchJoined => true;

        public virtual void Dispose()
        { }
    }
}
