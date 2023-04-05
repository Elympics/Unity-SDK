using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GameEngineCore.V1._1;
using GameEngineCore.V1._3;
using UnityEngine;
using Debug = UnityEngine.Debug;
using IGameEngine = GameEngineCore.V1._3.IGameEngine;

#pragma warning disable CS0618
#pragma warning disable CS0067

namespace Elympics
{
	public class GameEngineAdapter : IGameEngine
	{
		internal ElympicsPlayer[] Players { get; private set; } = Array.Empty<ElympicsPlayer>();

		public event Action<byte[], string>       InGameDataForPlayerOnReliableChannelGenerated;
		public event Action<byte[], string>       InGameDataForPlayerOnUnreliableChannelGenerated;
		public event Action<byte[]>               InGameDataForSpectatorsOnReliableChannelGenerated;
		public event Action<byte[]>               InGameDataForSpectatorsOnUnreliableChannelGenerated;
		public event Action<ResultMatchUserDatas> GameEnded;
		public event Action<ElympicsPlayer>       PlayerConnected;
		public event Action<ElympicsPlayer>       PlayerDisconnected;

		public event Action<InitialMatchPlayerDatasGuid> InitializedWithMatchPlayerDatas;

		private IGameEngineLogger                  _logger;
		private InitialMatchUserDatas              _initialMatchUserDatas;
		private Dictionary<string, ElympicsPlayer> _userIdsToPlayers;
		private Dictionary<ElympicsPlayer, string> _playersToUserIds;

		private readonly LogHandler _logHandler;
		private readonly int _playerInputBufferSize;

		public readonly ConcurrentDictionary<ElympicsPlayer, ElympicsInput> LatestSimulatedTickInput =
			new ConcurrentDictionary<ElympicsPlayer, ElympicsInput>();
		public ConcurrentDictionary<ElympicsPlayer, ElympicsDataWithTickBuffer<ElympicsInput>> PlayerInputBuffers { get; } =
			new ConcurrentDictionary<ElympicsPlayer, ElympicsDataWithTickBuffer<ElympicsInput>>();


		public GameEngineAdapter(ElympicsGameConfig elympicsGameConfig, LogHandler logHandler)
		{
			_playerInputBufferSize = elympicsGameConfig.PredictionBufferSize;
			_logHandler = logHandler;
		}

		public void Init(IGameEngineLogger logger, InitialMatchData initialMatchData)
		{
			_logger = logger;
			_logHandler.Logger = _logger;
		}

		public void Init2(InitialMatchUserDatas initialMatchUserDatas)
		{
			Players = Enumerable.Range(0, initialMatchUserDatas.Count).Select(ElympicsPlayer.FromIndex).ToArray();

			var userIds = initialMatchUserDatas.Select(userData => userData.UserId).ToList();

			_playersToUserIds = ElympicsPlayerAssociations.GetPlayersToUserIds(userIds);
			_userIdsToPlayers = ElympicsPlayerAssociations.GetUserIdsToPlayers(userIds);

			foreach (var userId in userIds)
				PlayerInputBuffers[_userIdsToPlayers[userId]] = new ElympicsDataWithTickBuffer<ElympicsInput>(_playerInputBufferSize);

			_initialMatchUserDatas = initialMatchUserDatas;
			InitializedWithMatchPlayerDatas?.Invoke(new InitialMatchPlayerDatasGuid(initialMatchUserDatas.Select(x => new InitialMatchPlayerDataGuid
			{
				Player = _userIdsToPlayers[x.UserId],
				UserId = new Guid(x.UserId),
				IsBot = x.IsBot,
				BotDifficulty = x.BotDifficulty,
				GameEngineData = x.GameEngineData,
				MatchmakerData = x.MatchmakerData
			}).ToList()));
			// _logger.Info("Initialized from unity");
		}

		public void OnInGameDataFromPlayerReliableReceived(byte[] data, string userId)   => AddReliableInputToBuffer(data, userId);
		public void OnInGameDataFromPlayerUnreliableReceived(byte[] data, string userId) => AddUnreliableInputsToBuffer(data, userId);

		private void AddReliableInputToBuffer(byte[] data, string userId)
		{
			var player = _userIdsToPlayers[userId];
			var input = data.Deserialize<ElympicsInput>();
			AddInputToBuffer(input, player);
		}

		private void AddUnreliableInputsToBuffer(byte[] data, string userId)
		{
			var player = _userIdsToPlayers[userId];

			var inputs = data.DeserializeList<ElympicsInput>();

			foreach (var input in inputs)
				AddInputToBuffer(input, player);
		}

		private void AddInputToBuffer(ElympicsInput input, ElympicsPlayer player)
		{
			input.Player = player;
			if (!PlayerInputBuffers.TryGetValue(player, out var buffer))
			{
				Debug.LogWarning($"Input buffer for {player} not found");
				return;
			}

			buffer.TryAddData(input);
		}

		internal void AddBotsOrClientsInServerInputToBuffer(ElympicsInput input, ElympicsPlayer player) => AddInputToBuffer(input, player);

		public void OnPlayerConnected(string userId)    => PlayerConnected?.Invoke(_userIdsToPlayers[userId]);
		public void OnPlayerDisconnected(string userId) => PlayerDisconnected?.Invoke(_userIdsToPlayers[userId]);

		public void Tick(long tick)
		{
			// _logger.Info($"Hello from unity tick {tick}");
			/* Using unity FixedUpdate */
		}

		public void SetLatestSimulatedInputTick(ElympicsPlayer player, ElympicsInput elympicsInput)
		{
			LatestSimulatedTickInput[player] = elympicsInput;
		}

		public void SendSnapshotUnreliable(ElympicsSnapshot snapshot)
		{
			var serializedData = snapshot.Serialize();
			foreach (var userData in _initialMatchUserDatas)
				InGameDataForPlayerOnUnreliableChannelGenerated?.Invoke(serializedData, userData.UserId);
		}

		public void SendSnapshotsUnreliable(Dictionary<ElympicsPlayer, ElympicsSnapshot> snapshots)
		{
			foreach (var (player, snapshot) in snapshots)
			{
				var userId = _playersToUserIds[player];
				var serializedData = snapshot.Serialize();
				InGameDataForPlayerOnUnreliableChannelGenerated?.Invoke(serializedData, userId);
			}
		}

		public void EndGame(ResultMatchPlayerDatas result = null)
		{
			try
			{
				if (result == null)
				{
					GameEnded?.Invoke(null);
					return;
				}

				if (result.Count != _initialMatchUserDatas.Count)
				{
					_logger.Error("Invalid length of match result, expected {0}, has {1}", _initialMatchUserDatas.Count, result.Count);
					GameEnded?.Invoke(null);
					return;
				}

				var matchResult = new ResultMatchUserDatas();
				for (var i = 0; i < result.Count; i++)
				{
					var userId = _playersToUserIds[Players[i]];
					matchResult.Add(new ResultMatchUserData
					{
						UserId = userId,
						GameEngineData = result[i].GameEngineData,
						MatchmakerData = result[i].MatchmakerData
					});
				}

				GameEnded?.Invoke(matchResult);
			}
			finally
			{
				Application.Quit(result == null ? 1 : 0);
			}
		}

		public event Action                  GameStarted;
		public event Action<List<GameEvent>> GameEventsGathered;

		event Action<MatchResult> GameEngineCore.V1._1.IGameEngine.GameEnded
		{
			add => throw new NotImplementedException();
			remove => throw new NotImplementedException();
		}
	}
}
