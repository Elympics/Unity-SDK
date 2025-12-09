using System;
using Elympics.ElympicsSystems.Internal;
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
            if (LobbyRegister.IsLobbyRegistered())
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

        private static GameSceneInitializer LoadFromLobbyClient() => LobbyRegister.GetJoinedMatchMode() switch
        {
            JoinedMatchMode.Online => new OnlineGameClientInitializer(),
            JoinedMatchMode.HalfRemoteClient => new HalfRemoteGameClientInitializer(),
            JoinedMatchMode.HalfRemoteServer => new HalfRemoteGameServerInitializer(),
            JoinedMatchMode.Local => new LocalGameServerInitializer(),
            JoinedMatchMode.SinglePlayer => new SinglePlayerGameInitializer(),
            JoinedMatchMode.SnapshotReplay => new PlayerSnapshotReplayInitializer(),
            _ => throw new ArgumentOutOfRangeException(LobbyRegister.GetJoinedMatchMode().ToString()),
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
