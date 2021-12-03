using System.Net;
using GameEngineCore.V1._3;
using UnityConnectors.HalfRemote;
using Debug = UnityEngine.Debug;

namespace Elympics
{
	internal class HalfRemoteGameServerInitializer : GameServerInitializer
	{
		private HalfRemoteGameEngineProtoConnector _halfRemoteGameEngineProtoConnector;

		protected override void InitializeGameServer(ElympicsGameConfig elympicsGameConfig, GameEngineAdapter gameEngineAdapter)
		{
			_halfRemoteGameEngineProtoConnector = new HalfRemoteGameEngineProtoConnector(
				gameEngineAdapter,
				new IPEndPoint(IPAddress.Any, elympicsGameConfig.TcpPortForHalfRemoteMode),
				new IPEndPoint(IPAddress.Any, elympicsGameConfig.WebPortForHalfRemoteMode),
				elympicsGameConfig.UseWebRtcInHalfRemote);
			_halfRemoteGameEngineProtoConnector.ReliableClientConnected += (clientId) => Debug.Log($"Reliable client {clientId} connected");
			_halfRemoteGameEngineProtoConnector.ReliableClientReceivingError += (clientId, error) => Debug.Log($"Reliable client {clientId} receiving error - {error}");
			_halfRemoteGameEngineProtoConnector.ReliableClientReceivingEnded += (clientId) => Debug.Log($"Reliable client {clientId} receiving ended");
			_halfRemoteGameEngineProtoConnector.UnreliableClientConnected += (clientId) => Debug.Log($"Unreliable client {clientId} connected");
			_halfRemoteGameEngineProtoConnector.UnreliableClientReceivingError += (clientId, error) => Debug.Log($"Unreliable client {clientId} receiving error - {error}");
			_halfRemoteGameEngineProtoConnector.UnreliableClientReceivingEnded += (clientId) => Debug.Log($"Unreliable client {clientId} receiving ended");
			_halfRemoteGameEngineProtoConnector.ListeningEnded += () => Debug.Log($"Listening ended");
			_halfRemoteGameEngineProtoConnector.ListeningError += (error) => Debug.Log($"Listening error {error}");

			var initialMatchData = new InitialMatchUserDatas(DebugPlayerListCreator.CreatePlayersList(elympicsGameConfig));
			gameEngineAdapter.Init(new LoggerNoop(), null);
			gameEngineAdapter.Init2(initialMatchData);
			_halfRemoteGameEngineProtoConnector.Listen();
		}

		public override void Dispose()
		{
			_halfRemoteGameEngineProtoConnector?.Dispose();
		}
	}
}
