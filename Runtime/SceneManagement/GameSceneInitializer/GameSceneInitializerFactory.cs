using System;
using UnityEngine;

namespace Elympics
{
    internal static class GameSceneInitializerFactory
    {
        public static GameSceneInitializer Create(ElympicsGameConfig elympicsGameConfig)
        {
            #region Build run

            if (ApplicationParameters.ShouldLoadElympicsOnlineBot)
                return new OnlineGameBotInitializer();
            if (ApplicationParameters.ShouldLoadElympicsOnlineServer)
                return new OnlineGameServerInitializer();
            if (ElympicsLobbyClient.Instance != null)
                return LoadFromLobbyClient();

            if (ScriptingSymbols.IsUnityServer && !Application.isEditor)
                return new HalfRemoteGameServerInitializer();

            #endregion

            return elympicsGameConfig.GameplaySceneDebugMode switch
            {
                ElympicsGameConfig.GameplaySceneDebugModeEnum.LocalPlayerAndBots => InitializeLocalPlayerAndBots(),
                ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote => InitializeHalfRemotePlayers(elympicsGameConfig),
                ElympicsGameConfig.GameplaySceneDebugModeEnum.DebugOnlinePlayer => InitializeDebugOnlinePlayer(),
                _ => throw new ArgumentOutOfRangeException(nameof(elympicsGameConfig.GameplaySceneDebugMode)),
            };
        }

        private static GameSceneInitializer LoadFromLobbyClient()
        {
            return ElympicsLobbyClient.Instance.MatchMode switch
            {
                ElympicsLobbyClient.JoinedMatchMode.Online => new OnlineGameClientInitializer(),
                ElympicsLobbyClient.JoinedMatchMode.HalfRemoteClient => new HalfRemoteGameClientInitializer(),
                ElympicsLobbyClient.JoinedMatchMode.HalfRemoteServer => new HalfRemoteGameServerInitializer(),
                ElympicsLobbyClient.JoinedMatchMode.Local => InitializeLocalPlayerAndBots(),
                _ => throw new ArgumentOutOfRangeException(nameof(ElympicsLobbyClient.Instance.MatchMode)),
            };
        }

        private static GameSceneInitializer InitializeLocalPlayerAndBots() => new LocalGameServerInitializer();
        private static DebugOnlineClientInitializer InitializeDebugOnlinePlayer() => new();

        private static GameSceneInitializer InitializeHalfRemotePlayers(ElympicsGameConfig elympicsGameConfig)
        {
            return elympicsGameConfig.HalfRemoteMode switch
            {
                ElympicsGameConfig.HalfRemoteModeEnum.Server => new HalfRemoteGameServerInitializer(),
                ElympicsGameConfig.HalfRemoteModeEnum.Client => new HalfRemoteGameClientInitializer(),
                ElympicsGameConfig.HalfRemoteModeEnum.Bot => new HalfRemoteGameBotInitializer(),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }
}
