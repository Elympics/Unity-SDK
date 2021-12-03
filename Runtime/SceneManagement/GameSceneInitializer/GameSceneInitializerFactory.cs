using System;
using static Elympics.ApplicationParameters.Factory;

namespace Elympics
{
	internal static class GameSceneInitializerFactory
	{
		private const string ElympicsEnvironmentVariable    = "ELYMPICS";
		private const string ElympicsBotEnvironmentVariable = "ELYMPICS_BOT";

		public static GameSceneInitializer Create(ElympicsGameConfig elympicsGameConfig)
		{
			#region Build run

			if (ShouldLoadElympicsOnlineBot())
				return new OnlineGameBotInitializer();
			if (ShouldLoadElympicsOnlineServer())
				return new OnlineGameServerInitializer();
			if (ShouldLoadElympicsOnlineClient())
				return new OnlineGameClientInitializer();

			if (ShouldLoadHalfRemoteServer())
				return new HalfRemoteGameServerInitializer();
			if (ShouldLoadHalfRemoteClient())
				return new HalfRemoteGameClientInitializer();

			#endregion

			switch (elympicsGameConfig.GameplaySceneDebugMode)
			{
				case ElympicsGameConfig.GameplaySceneDebugModeEnum.LocalPlayerAndBots:
					return InitializeLocalPlayerAndBots();
				case ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote:
					return InitializeHalfRemotePlayers(elympicsGameConfig);
				case ElympicsGameConfig.GameplaySceneDebugModeEnum.DebugOnlinePlayer:
					return InitializeDebugOnlinePlayer();
				default:
					throw new ArgumentOutOfRangeException(nameof(elympicsGameConfig.GameplaySceneDebugMode));
			}
		}

		private static GameSceneInitializer         InitializeLocalPlayerAndBots() => new LocalGameServerInitializer();
		private static DebugOnlineClientInitializer InitializeDebugOnlinePlayer()  => new DebugOnlineClientInitializer();

		private static GameSceneInitializer InitializeHalfRemotePlayers(ElympicsGameConfig elympicsGameConfig)
		{
			switch (elympicsGameConfig.HalfRemoteMode)
			{
				case ElympicsGameConfig.HalfRemoteModeEnum.Server:
					return new HalfRemoteGameServerInitializer();
				case ElympicsGameConfig.HalfRemoteModeEnum.Client:
					return new HalfRemoteGameClientInitializer();
				case ElympicsGameConfig.HalfRemoteModeEnum.Bot:
					return new HalfRemoteGameBotInitializer();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
