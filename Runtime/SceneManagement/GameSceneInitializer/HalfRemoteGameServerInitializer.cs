using System;
using System.Net;
using System.Threading;
using Elympics.Core.Logger;
using Elympics.ElympicsSystems;
using Elympics.Replication;
using Elympics.SnapshotAnalysis;
using GameEngineCore;
using UnityConnectors.HalfRemote;
using SimpleHttpSignalingServer = Plugins.Elympics.Runtime.Communication.HalfRemote.SimpleHttpSignalingServer;

namespace Elympics
{
    internal class HalfRemoteGameServerInitializer : GameServerInitializer
    {
        /// <summary>The launch settings of this server.</summary>
        internal HalfRemoteServerLaunchData Data { get; }

        private SimpleHttpSignalingServer _signalingServer;
        private CancellationTokenSource _signalingServerCts;
        private HalfRemoteGameEngineProtoConnector _halfRemoteGameEngineProtoConnector;
        private CancellationTokenSource _systemToken;

        public HalfRemoteGameServerInitializer(HalfRemoteServerLaunchData data) => Data = data;

        protected override void InitializeGameServer(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter)
        {
            _systemToken = new CancellationTokenSource();
            // Launcher-driven: report the match end and let the host return to boot. Otherwise quit, as before.
            var sink = LifecycleSink;
            Action onAfterGameEnded = sink == null ? null : () => sink.MatchEnded(Guid.Empty);
            _halfRemoteGameEngineProtoConnector = new HalfRemoteGameEngineProtoConnector(
                gameEngineAdapter,
                new IPEndPoint(IPAddress.Parse(elympicsGameConfig.IpForHalfRemoteMode), elympicsGameConfig.TcpPortForHalfRemoteMode),
                new IPEndPoint(IPAddress.Parse(elympicsGameConfig.IpForHalfRemoteMode), elympicsGameConfig.WebPortForHalfRemoteMode),
                onAfterGameEnded);
            _halfRemoteGameEngineProtoConnector.ReliableClientConnected += clientId =>
                ElympicsLogger.LogInfo($"Client {clientId} connected using reliable channel.");
            _halfRemoteGameEngineProtoConnector.ReliableClientReceivingError += (clientId, error) =>
                ElympicsLogger.LogError($"Error receiving from client {clientId} using reliable channel: {error}");
            _halfRemoteGameEngineProtoConnector.ReliableClientReceivingEnded += clientId =>
                ElympicsLogger.LogInfo($"Receiving from {clientId} using reliable channel stopped.");
            _halfRemoteGameEngineProtoConnector.UnreliableClientConnected += clientId =>
                ElympicsLogger.LogInfo($"Client {clientId} connected using unreliable channel.");
            _halfRemoteGameEngineProtoConnector.UnreliableClientReceivingError += (clientId, error) =>
                ElympicsLogger.LogInfo($"Error receiving from client {clientId} using unreliable channel: {error}");
            _halfRemoteGameEngineProtoConnector.UnreliableClientReceivingEnded += clientId =>
                ElympicsLogger.LogInfo($"Receiving from {clientId} using unreliable channel stopped.");
            _halfRemoteGameEngineProtoConnector.ListeningEnded += source =>
                ElympicsLogger.LogInfo($"{source} stopped listening.");
            _halfRemoteGameEngineProtoConnector.ListeningError += x =>
                ElympicsLogger.LogInfo($"Listening error for {x.Source}: {x.Error}");

            // ReplicationPipeline + PipelineBuffers only needed on server
            ReplicationPipeline.Initialize(elympicsGameConfig.MaxPlayers, ElympicsWorld.Current);
            gameEngineAdapter.Initialize(new InitialMatchData { UserData = DebugPlayerListCreator.CreatePlayersList(elympicsGameConfig) });

            _signalingServer = new SimpleHttpSignalingServer(_halfRemoteGameEngineProtoConnector,
                new IPEndPoint(IPAddress.Parse(elympicsGameConfig.IpForHalfRemoteMode), elympicsGameConfig.WebPortForHalfRemoteMode));
            _signalingServerCts = new CancellationTokenSource();

            _halfRemoteGameEngineProtoConnector.Listen();
            _signalingServer.RunAsync(_signalingServerCts.Token).Forget();
        }
        protected override SnapshotAnalysisCollector ProvideSnapSnapshotAnalysisCollector() =>
            GameConfig.RecordSnapshots ? new EditorSnapshotAnalysisCollector(GameConfig.SnapshotFilePath) : new NullSnapshotAnalysisCollector();
        protected override IServerPlayerHandler ProvideInputRetriever() => new NullServerPlayerHandler();
        protected override IServerElympicsUpdateLoop ProvideElympicsUpdateLoop() => new DefaultServerElympicsUpdateLoop(BehavioursManager, GameEngineAdapter, Server, GameConfig);

        public override void Dispose()
        {
            base.Dispose();
            _systemToken?.Cancel();
            _systemToken?.Dispose();
            _systemToken = null;
            _halfRemoteGameEngineProtoConnector?.Dispose();
            _halfRemoteGameEngineProtoConnector = null;
            _signalingServerCts?.Cancel();
            _signalingServerCts?.Dispose();
            _signalingServerCts = null;
            _signalingServer?.Dispose();
            _signalingServer = null;
        }
    }
}
