using System;
using GameBotCore.V1._1;
using UnityEngine;
using BotConfiguration = GameBotCore.V1._3.BotConfiguration;
using IGameBot = GameBotCore.V1._3.IGameBot;

namespace Elympics
{
	public class GameBotAdapter : IGameBot
	{
		internal bool           Initialized { get; private set; }
		internal ElympicsPlayer Player      { get; private set; }

		private IGameBotLogger _logger;

		public event Action<InitialMatchPlayerDataGuid> InitializedWithMatchPlayerData;

		public event Action<byte[]> InGameDataForReliableChannelGenerated;
		public event Action<byte[]> InGameDataForUnreliableChannelGenerated;

		public void OnInGameDataUnreliableReceived(byte[] data)
		{
			var snapshot = data.Deserialize<ElympicsSnapshot>();
			SnapshotReceived?.Invoke(snapshot);
		}

		public void OnInGameDataReliableReceived(byte[] data)
		{
			var snapshot = data.Deserialize<ElympicsSnapshot>();
			SnapshotReceived?.Invoke(snapshot);
		}

		public void Init(IGameBotLogger logger, GameBotCore.V1._1.BotConfiguration botConfiguration)
		{
			_logger = logger;
			Application.logMessageReceived += OnLogMessageReceived;
		}

		private void OnLogMessageReceived(string condition, string trace, LogType type)
		{
			switch (type)
			{
				case LogType.Error:
					_logger.Error(condition);
					break;
				case LogType.Assert:
					_logger.Fatal(condition);
					break;
				case LogType.Warning:
					_logger.Warning(condition);
					break;
				case LogType.Log:
					_logger.Info(condition);
					break;
				case LogType.Exception:
					_logger.Error(condition);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}

		public void Init2(GameBotCore.V1._2.BotConfiguration botConfiguration)
		{
		}

		public void Init3(BotConfiguration botConfiguration)
		{
			Player = ElympicsPlayer.FromIndex(botConfiguration.MatchPlayers.IndexOf(botConfiguration.UserId));
			InitializedWithMatchPlayerData?.Invoke(new InitialMatchPlayerDataGuid
			{
				Player = Player,
				IsBot = true,
				BotDifficulty = botConfiguration.Difficulty,
				MatchmakerData = botConfiguration.MatchmakerData,
				GameEngineData = botConfiguration.GameEngineData
			});
			Initialized = true;
		}

		public void Tick(long tick)
		{
		}

		internal event Action<ElympicsSnapshot> SnapshotReceived;
		internal void                           SendInputReliable(ElympicsInput input)   => InGameDataForReliableChannelGenerated?.Invoke(input.Serialize());
		internal void                           SendInputUnreliable(ElympicsInput input) => InGameDataForUnreliableChannelGenerated?.Invoke(input.Serialize());
	}
}
