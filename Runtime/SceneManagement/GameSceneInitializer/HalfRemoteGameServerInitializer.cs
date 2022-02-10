using System.Net;
using System.Threading;
using GameEngineCore.V1._3;
using UnityConnectors.HalfRemote;
using Debug = UnityEngine.Debug;
using SimpleHttpSignalingServer = Plugins.Elympics.Runtime.Communication.HalfRemote.SimpleHttpSignalingServer;

namespace Elympics
{
	internal class HalfRemoteGameServerInitializer : GameServerInitializer
	{
		private SimpleHttpSignalingServer          _signalingServer;
		private CancellationTokenSource            _signalingServerCts;
		private HalfRemoteGameEngineProtoConnector _halfRemoteGameEngineProtoConnector;

		protected override void InitializeGameServer(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter)
		{
			_halfRemoteGameEngineProtoConnector = new HalfRemoteGameEngineProtoConnector(
				gameEngineAdapter,
				new IPEndPoint(IPAddress.Parse(elympicsGameConfig.IpForHalfRemoteMode), elympicsGameConfig.TcpPortForHalfRemoteMode),
				new IPEndPoint(IPAddress.Parse(elympicsGameConfig.IpForHalfRemoteMode), elympicsGameConfig.WebPortForHalfRemoteMode));
			_halfRemoteGameEngineProtoConnector.ReliableClientConnected += (clientId) => Debug.Log($"Reliable client {clientId} connected");
			_halfRemoteGameEngineProtoConnector.ReliableClientReceivingError += (clientId, error) => Debug.Log($"Reliable client {clientId} receiving error - {error}");
			_halfRemoteGameEngineProtoConnector.ReliableClientReceivingEnded += (clientId) => Debug.Log($"Reliable client {clientId} receiving ended");
			_halfRemoteGameEngineProtoConnector.UnreliableClientConnected += (clientId) => Debug.Log($"Unreliable client {clientId} connected");
			_halfRemoteGameEngineProtoConnector.UnreliableClientReceivingError += (clientId, error) => Debug.Log($"Unreliable client {clientId} receiving error - {error}");
			_halfRemoteGameEngineProtoConnector.UnreliableClientReceivingEnded += (clientId) => Debug.Log($"Unreliable client {clientId} receiving ended");
			_halfRemoteGameEngineProtoConnector.ListeningEnded += (source) => Debug.Log($"{source} Listening ended");
			_halfRemoteGameEngineProtoConnector.ListeningError += (source, error) => Debug.Log($"{source} Listening error {error}");

			var initialMatchData = new InitialMatchUserDatas(DebugPlayerListCreator.CreatePlayersList(elympicsGameConfig));
			gameEngineAdapter.Init(new LoggerNoop(), null);
			gameEngineAdapter.Init2(initialMatchData);
			
			_signalingServer = new SimpleHttpSignalingServer(_halfRemoteGameEngineProtoConnector, new IPEndPoint(IPAddress.Parse(elympicsGameConfig.IpForHalfRemoteMode), elympicsGameConfig.WebPortForHalfRemoteMode));
			_signalingServerCts = new CancellationTokenSource();
			
			_halfRemoteGameEngineProtoConnector.Listen();
			_signalingServer.RunAsync(_signalingServerCts.Token);
		}

		public override void Dispose()
		{
			_halfRemoteGameEngineProtoConnector?.Dispose();
			_signalingServerCts?.Cancel();
		}
	}
}
