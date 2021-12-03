using static Elympics.ApplicationParameters.HalfRemote;

namespace Elympics
{
	internal class HalfRemoteGameClientInitializer : GameClientInitializer
	{
		private HalfRemoteMatchClientAdapter _halfRemoteMatchClient;
		private HalfRemoteMatchConnectClient _halfRemoteMatchConnectClient;

		protected override void InitializeClient(ElympicsClient client, ElympicsGameConfig elympicsGameConfig)
		{
			var playerIndex = GetPlayerIndex(elympicsGameConfig);
			var ip = GetIp(elympicsGameConfig);
			var port = GetPort(elympicsGameConfig);

			var playersList = DebugPlayerListCreator.CreatePlayersList(elympicsGameConfig);

			var userId = playersList[playerIndex].UserId;
			var matchmakerData = playersList[playerIndex].MatchmakerData;
			var gameEngineData = playersList[playerIndex].GameEngineData;

			_halfRemoteMatchClient = new HalfRemoteMatchClientAdapter(elympicsGameConfig);
			_halfRemoteMatchConnectClient = new HalfRemoteMatchConnectClient(_halfRemoteMatchClient, ip, port, userId, elympicsGameConfig.UseWebSocketsInHalfRemote, elympicsGameConfig.UseWebRtcInHalfRemote);
			client.InitializeInternal(elympicsGameConfig, _halfRemoteMatchConnectClient, _halfRemoteMatchClient,
				new InitialMatchPlayerData
				{
					Player = ElympicsPlayer.FromIndex(playerIndex),
					UserId = userId,
					IsBot = false,
					MatchmakerData = matchmakerData,
					GameEngineData = gameEngineData
				});
		}

		public override void Dispose()
		{
			_halfRemoteMatchConnectClient?.Disconnect();
			_halfRemoteMatchClient?.Dispose();
		}
	}
}
