using System;
using UnityEngine;

namespace Elympics
{
    internal static class GameSceneInitializerFactory
    {
        public static GameSceneInitializer Create(ElympicsGameConfig elympicsGameConfig)
        {
            if (ApplicationParameters.ShouldLoadElympicsOnlineBot)
                return new OnlineGameBotInitializer();
            if (ApplicationParameters.ShouldLoadElympicsOnlineServer)
                return new OnlineGameServerInitializer();
            if (ElympicsLobbyClient.Instance != null)
                return LoadFromLobbyClient();

            if (ScriptingSymbols.IsUnityServer && !Application.isEditor)
                return new HalfRemoteGameServerInitializer();


            return elympicsGameConfig.GameplaySceneDebugMode switch
            {
                ElympicsGameConfig.GameplaySceneDebugModeEnum.LocalPlayerAndBots => new LocalGameServerInitializer(),
                ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote => InitializeHalfRemotePlayers(elympicsGameConfig),
                ElympicsGameConfig.GameplaySceneDebugModeEnum.DebugOnlinePlayer => InitializeDebugOnlinePlayer(),
                ElympicsGameConfig.GameplaySceneDebugModeEnum.SnapshotReplay => new EditorSnapshotReplayInitializer(),
                ElympicsGameConfig.GameplaySceneDebugModeEnum.SinglePlayer => new SinglePlayerGameInitializer(),
                _ => throw new ArgumentOutOfRangeException(nameof(elympicsGameConfig.GameplaySceneDebugMode)),
            };
        }

        private static GameSceneInitializer LoadFromLobbyClient() => ElympicsLobbyClient.Instance!.MatchMode switch
        {
            ElympicsLobbyClient.JoinedMatchMode.Online => new OnlineGameClientInitializer(),
            ElympicsLobbyClient.JoinedMatchMode.HalfRemoteClient => new HalfRemoteGameClientInitializer(),
            ElympicsLobbyClient.JoinedMatchMode.HalfRemoteServer => new HalfRemoteGameServerInitializer(),
            ElympicsLobbyClient.JoinedMatchMode.Local => new LocalGameServerInitializer(),
            ElympicsLobbyClient.JoinedMatchMode.SinglePlayer => new SinglePlayerGameInitializer(),
            ElympicsLobbyClient.JoinedMatchMode.SnapshotReplay => new PlayerSnapshotReplayInitializer(),
            _ => throw new ArgumentOutOfRangeException(nameof(ElympicsLobbyClient.Instance.MatchMode)),
        };

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
