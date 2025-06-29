using System.Net;
using System.Threading;
using Elympics.ElympicsSystems;
using Elympics.SnapshotAnalysis;
using GameEngineCore.V1._4;
using UnityConnectors.HalfRemote;
using SimpleHttpSignalingServer = Plugins.Elympics.Runtime.Communication.HalfRemote.SimpleHttpSignalingServer;

namespace Elympics
{
    internal class HalfRemoteGameServerInitializer : GameServerInitializer
    {
        private SimpleHttpSignalingServer _signalingServer;
        private CancellationTokenSource _signalingServerCts;
        private HalfRemoteGameEngineProtoConnector _halfRemoteGameEngineProtoConnector;
        private CancellationTokenSource _systemToken;

        protected override void InitializeGameServer(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter)
        {
            _systemToken = new CancellationTokenSource();
            _halfRemoteGameEngineProtoConnector = new HalfRemoteGameEngineProtoConnector(
                gameEngineAdapter,
                new IPEndPoint(IPAddress.Parse(elympicsGameConfig.IpForHalfRemoteMode), elympicsGameConfig.TcpPortForHalfRemoteMode),
                new IPEndPoint(IPAddress.Parse(elympicsGameConfig.IpForHalfRemoteMode), elympicsGameConfig.WebPortForHalfRemoteMode));
            _halfRemoteGameEngineProtoConnector.ReliableClientConnected += clientId =>
                ElympicsLogger.Log($"Client {clientId} connected using reliable channel.");
            _halfRemoteGameEngineProtoConnector.ReliableClientReceivingError += (clientId, error) =>
                ElympicsLogger.LogError($"Error receiving from client {clientId} using reliable channel: {error}");
            _halfRemoteGameEngineProtoConnector.ReliableClientReceivingEnded += clientId =>
                ElympicsLogger.Log($"Receiving from {clientId} using reliable channel stopped.");
            _halfRemoteGameEngineProtoConnector.UnreliableClientConnected += clientId =>
                ElympicsLogger.Log($"Client {clientId} connected using unreliable channel.");
            _halfRemoteGameEngineProtoConnector.UnreliableClientReceivingError += (clientId, error) =>
                ElympicsLogger.Log($"Error receiving from client {clientId} using unreliable channel: {error}");
            _halfRemoteGameEngineProtoConnector.UnreliableClientReceivingEnded += clientId =>
                ElympicsLogger.Log($"Receiving from {clientId} using unreliable channel stopped.");
            _halfRemoteGameEngineProtoConnector.ListeningEnded += source =>
                ElympicsLogger.Log($"{source} stopped listening.");
            _halfRemoteGameEngineProtoConnector.ListeningError += x =>
                ElympicsLogger.Log($"Listening error for {x.Source}: {x.Error}");

            gameEngineAdapter.Initialize(new InitialMatchData { UserData = DebugPlayerListCreator.CreatePlayersList(elympicsGameConfig) });

            _signalingServer = new SimpleHttpSignalingServer(_halfRemoteGameEngineProtoConnector,
                new IPEndPoint(IPAddress.Parse(elympicsGameConfig.IpForHalfRemoteMode), elympicsGameConfig.WebPortForHalfRemoteMode));
            _signalingServerCts = new CancellationTokenSource();

            _halfRemoteGameEngineProtoConnector.Listen();
            _signalingServer.RunAsync(_signalingServerCts.Token);
        }
        protected override SnapshotAnalysisCollector ProvideSnapSnapshotAnalysisCollector() =>
            GameConfig.RecordSnapshots ? new EditorSnapshotAnalysisCollector(GameConfig.SnapshotFilePath) : new NullSnapshotAnalysisCollector();
        protected override IServerPlayerHandler ProvideInputRetriever() => new NullServerPlayerHandler();
        protected override IServerElympicsUpdateLoop ProvideElympicsUpdateLoop() => new DefaultServerElympicsUpdateLoop(BehavioursManager, GameEngineAdapter, Server, GameConfig);

        public override void Dispose()
        {
            _systemToken.Cancel();
            _halfRemoteGameEngineProtoConnector?.Dispose();
            _signalingServerCts?.Cancel();
        }
    }
}
