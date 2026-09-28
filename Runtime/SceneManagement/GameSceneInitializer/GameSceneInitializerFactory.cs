#nullable enable

using System;
using Elympics.ElympicsSystems.Internal;
using UnityEngine;

namespace Elympics
{
    internal static class GameSceneInitializerFactory
    {
        public static GameSceneInitializer Create(ElympicsGameConfig elympicsGameConfig, ElympicsLaunch? launch)
        {
            if (ApplicationParameters.ShouldLoadElympicsOnlineServer)
                return new OnlineGameServerInitializer();
            if (launch != null)
                return CreateForLaunch(launch);

            if (ScriptingSymbols.IsUnityServer && !Application.isEditor)
                return new HalfRemoteGameServerInitializer();


            return elympicsGameConfig.GameplaySceneDebugMode switch
            {
                ElympicsGameConfig.GameplaySceneDebugModeEnum.HalfRemote => InitializeHalfRemotePlayers(elympicsGameConfig),
                ElympicsGameConfig.GameplaySceneDebugModeEnum.SnapshotReplay when Application.isEditor => new EditorSnapshotReplayInitializer(),
                ElympicsGameConfig.GameplaySceneDebugModeEnum.SinglePlayer => new SinglePlayerGameInitializer(null),
                _ => throw new ArgumentOutOfRangeException(nameof(elympicsGameConfig.GameplaySceneDebugMode)),
            };
        }

        private static GameSceneInitializer CreateForLaunch(ElympicsLaunch launch) => launch.MatchMode switch
        {
            JoinedMatchMode.Online => new OnlineGameClientInitializer(
                launch.MatchData ?? throw new ElympicsException("Online launch without match data."), launch.UserId),
            JoinedMatchMode.HalfRemoteClient => new HalfRemoteGameClientInitializer(),
            JoinedMatchMode.HalfRemoteServer => new HalfRemoteGameServerInitializer(),
            JoinedMatchMode.SinglePlayer => new SinglePlayerGameInitializer(launch.MatchData),
            JoinedMatchMode.SnapshotReplay => new PlayerSnapshotReplayInitializer(
                launch.SnapshotRetriever ?? throw new ElympicsException("Snapshot replay launch without a snapshot retriever.")),
            _ => throw new ArgumentOutOfRangeException(nameof(launch.MatchMode), launch.MatchMode.ToString()),
        };

        private static GameSceneInitializer InitializeHalfRemotePlayers(ElympicsGameConfig elympicsGameConfig)
        {
            return elympicsGameConfig.HalfRemoteMode switch
            {
                ElympicsGameConfig.HalfRemoteModeEnum.Server => new HalfRemoteGameServerInitializer(),
                ElympicsGameConfig.HalfRemoteModeEnum.Client => new HalfRemoteGameClientInitializer(),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }
}
