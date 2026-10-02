#nullable enable

using System;
using Elympics.SnapshotAnalysis.Retrievers;

namespace Elympics
{
    internal sealed class GameSceneLaunchContext
    {
        public ElympicsClient Client { get; }
        public ElympicsServer Server { get; }
        public ElympicsGameConfig GameConfig { get; }
        public ElympicsBehavioursManager BehavioursManager { get; }
        /// <summary>Set only for snapshot replays.</summary>
        public SnapshotAnalysisRetriever? SnapshotRetriever { get; }

        private readonly Action<GameSceneInitializer> _adopt;

        /// <param name="adopt">Takes ownership of the initializer before it initializes, so it is disposed even if initializing throws.</param>
        public GameSceneLaunchContext(ElympicsClient client, ElympicsServer server, ElympicsGameConfig gameConfig, ElympicsBehavioursManager behavioursManager,
            SnapshotAnalysisRetriever? snapshotRetriever, Action<GameSceneInitializer> adopt)
        {
            Client = client;
            Server = server;
            GameConfig = gameConfig;
            BehavioursManager = behavioursManager;
            SnapshotRetriever = snapshotRetriever;
            _adopt = adopt;
        }

        public void Adopt(GameSceneInitializer initializer) => _adopt(initializer);
    }
}
