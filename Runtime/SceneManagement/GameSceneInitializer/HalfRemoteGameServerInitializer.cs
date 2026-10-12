using System;
using System.Linq;
using System.Threading;
using Elympics.Core.Logger;
using Elympics.ElympicsSystems;
using Elympics.SnapshotAnalysis;
using GameEngineCore;
using UnityConnectors.HalfRemote;
using SimpleHttpSignalingServer = Plugins.Elympics.Runtime.Communication.HalfRemote.SimpleHttpSignalingServer;

namespace Elympics
{
    internal class HalfRemoteGameServerInitializer : GameServerInitializer
    {
        private readonly HalfRemoteServerLaunchData _data;
        private SimpleHttpSignalingServer _signalingServer;
        private CancellationTokenSource _signalingServerCts;
        private HalfRemoteGameEngineProtoConnector _halfRemoteGameEngineProtoConnector;
        private CancellationTokenSource _systemToken;

        public HalfRemoteGameServerInitializer(HalfRemoteServerLaunchData data) => _data = data;

        public override void Initialize(ElympicsClient client, ElympicsServer server, ElympicsGameConfig gameConfig, ElympicsBehavioursManager behavioursManager)
        {
            server.IsHalfRemoteServer = true;
            base.Initialize(client, server, gameConfig, behavioursManager);
        }

        protected override void InitializeGameServer(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter)
        {
            _systemToken = new CancellationTokenSource();
            var settings = HalfRemoteServerSettings.Resolve(_data, elympicsGameConfig);
            foreach (var warning in settings.Warnings)
                ElympicsLogger.LogWarning(warning);
            // Null without a launcher session: the connector then quits the application.
            var sink = LifecycleSink;
            Action onAfterGameEnded = sink == null ? null : () => sink.MatchEnded(Guid.Empty);
            _halfRemoteGameEngineProtoConnector = new HalfRemoteGameEngineProtoConnector(
                gameEngineAdapter,
                settings.TcpUdpEndPoint,
                settings.WebEndPoint,
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

            var players = DebugPlayerListCreator.CreatePlayersList(elympicsGameConfig).Take(settings.PlayerCount).ToList();
            gameEngineAdapter.Initialize(new InitialMatchData { UserData = players });

            _halfRemoteGameEngineProtoConnector.Listen();
            ElympicsLogger.LogInfo($"Half-remote server listening on {(settings.TcpUdpEndPoint != null ? $"TCP/UDP {settings.TcpUdpEndPoint}" : "")}"
                + $"{(settings.TcpUdpEndPoint != null && settings.WebEndPoint != null ? " and " : "")}"
                + $"{(settings.WebEndPoint != null ? $"WebRTC signaling {settings.WebEndPoint}" : "")}, for {players.Count} players.");
            if (settings.WebEndPoint != null)
            {
                _signalingServer = new SimpleHttpSignalingServer(_halfRemoteGameEngineProtoConnector, settings.WebEndPoint);
                _signalingServerCts = new CancellationTokenSource();
                _signalingServer.RunAsync(_signalingServerCts.Token).Forget();
            }
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
