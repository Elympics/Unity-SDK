using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Proto.ProtoClient;
using Proto.ProtoClient.NetworkClient;
using Proto.ProtoClient.Receivers;
using ProtoGameBot;
using ProtoLog;
using ProtoUnityGameBot;
using IGameBot = GameBotCore.V1._3.IGameBot;
using BotConfiguration1 = GameBotCore.V1._1.BotConfiguration;
using BotConfiguration2 = GameBotCore.V1._2.BotConfiguration;
using BotConfiguration3 = GameBotCore.V1._3.BotConfiguration;
using InGameDataReliableReceivedMsg = ProtoUnityGameBot.InGameDataReliableReceivedMsg;
using InGameDataUnreliableReceivedMsg = ProtoUnityGameBot.InGameDataUnreliableReceivedMsg;

namespace UnityConnectors
{
	public class GameBotProtoConnector : IGameBotProtoReceiver, IDisposable
	{
		private const int GameBotPort = 50002;

		private readonly IGameBot       _gameBot;
		private readonly ProtoConnector _protoConnector;

		private GameBotProtoClient _client;

		public GameBotProtoConnector(IGameBot gameBot)
		{
			_gameBot = gameBot;
			_protoConnector = new ProtoConnector(IPAddress.Loopback, GameBotPort, CreateGameBotProtoClient);
		}

		private ProtoClient CreateGameBotProtoClient(TcpClient tcpClient)
		{
			var protoNetworkClient = new ProtoNetworkStreamClient(tcpClient.GetStream());
			_client = new GameBotProtoClient(protoNetworkClient, this);
			_client.ReceivingEnded += Dispose;

			_gameBot.InGameDataForReliableChannelGenerated += OnInGameDataForReliableChannelGenerated;
			_gameBot.InGameDataForUnreliableChannelGenerated += OnInGameDataForUnreliableChannelGenerated;

			_client.Receive();
			return _client;
		}

		public void Connect() => _protoConnector.Connect();

		private void OnInGameDataForReliableChannelGenerated(byte[] data)                  => _client.Send(new InGameDataForReliableChannelGeneratedMsg {Data = ByteString.CopyFrom(data)});
		private void OnInGameDataForUnreliableChannelGenerated(byte[] data)                => _client.Send(new InGameDataForUnreliableChannelGeneratedMsg {Data = ByteString.CopyFrom(data)});
		public  void InGameDataReliableReceived(InGameDataReliableReceivedMsg message)     => _gameBot.OnInGameDataReliableReceived(message.Data.ToByteArray());
		public  void InGameDataUnreliableReceived(InGameDataUnreliableReceivedMsg message) => _gameBot.OnInGameDataUnreliableReceived(message.Data.ToByteArray());

		public void Tick(TickMsg message)
		{
			_protoConnector.UpdateLastUpdateFromGameServer();
			_gameBot.Tick(message.Tick);
		}

		public void Init(BotConfiguration1Msg message)
		{
			_gameBot.Init(_protoConnector, new BotConfiguration1
			{
				MatchId = message.MatchId,
				UserId = message.UserId,
				Difficulty = message.Difficulty,
			});
		}

		public void Init2(BotConfiguration2Msg message)
		{
			var matchId = message.BotConfiguration1.MatchId;
			var userId = message.BotConfiguration1.UserId;
			var difficulty = message.BotConfiguration1.Difficulty;
			var matchPlayers = message.MatchPlayers.ToList();

			_gameBot.Init2(new BotConfiguration2
			{
				MatchId = matchId,
				UserId = userId,
				Difficulty = difficulty,
				MatchPlayers = matchPlayers
			});
		}

		public void Init3(BotConfiguration3Msg message)
		{
			var matchId = message.BotConfiguration2.BotConfiguration1.MatchId;
			var userId = message.BotConfiguration2.BotConfiguration1.UserId;
			var difficulty = message.BotConfiguration2.BotConfiguration1.Difficulty;
			var matchPlayers = message.BotConfiguration2.MatchPlayers.ToList();
			var matchmakerData = message.MatchmakerData.ToArray();
			var gameEngineData = message.GameEngineData.ToByteArray();

			_gameBot.Init3(new BotConfiguration3
			{
				MatchId = matchId,
				UserId = userId,
				Difficulty = difficulty,
				MatchPlayers = matchPlayers,
				MatchmakerData = matchmakerData,
				GameEngineData = gameEngineData
			});
			_client.Send(new InitializedMsg());
		}

		public void Dispose()
		{
			_protoConnector.Dispose();
			Environment.Exit(0);
		}

		public void Verbose(string message, params object[] arguments) => _client.Send(new LogVerboseMsg {Log = string.Format(message, arguments)});
		public void Debug(string message, params object[] arguments)   => _client.Send(new LogDebugMsg {Log = string.Format(message, arguments)});
		public void Info(string message, params object[] arguments)    => _client.Send(new LogInfoMsg {Log = string.Format(message, arguments)});
		public void Warning(string message, params object[] arguments) => _client.Send(new LogWarningMsg {Log = string.Format(message, arguments)});

		public void Warning(string message, Exception exception, params object[] arguments) =>
			_client.Send(new LogWarningMsg {Log = string.Format(message, arguments) + Environment.NewLine + exception});

		public void Error(string message, params object[] arguments)                      => _client.Send(new LogErrorMsg {Log = string.Format(message, arguments)});
		public void Error(string message, Exception exception, params object[] arguments) => _client.Send(new LogErrorMsg {Log = string.Format(message, arguments) + Environment.NewLine + exception});
		public void Fatal(string message, params object[] arguments)                      => _client.Send(new LogFatalMsg {Log = string.Format(message, arguments)});

		public void Fatal(string message, Exception exception, params object[] arguments)
		{
			_client.Send(new LogFatalMsg {Log = string.Format(message, arguments) + Environment.NewLine + exception});
		}
	}
}
