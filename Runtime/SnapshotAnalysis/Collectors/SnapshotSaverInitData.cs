#nullable enable
using System;
using System.Collections.Generic;
using MessagePack;

namespace Elympics.SnapshotAnalysis
{
    [MessagePackObject]
    public class SnapshotSaverInitData
    {
        [Key(0)] public readonly string SnapshotSaverVersion;
        [Key(1)] public readonly string GameName;
        [Key(2)] public readonly string GameId;
        [Key(3)] public readonly string GameVersion;
        [Key(4)] public readonly int Players;
        [Key(5)] public readonly string SdkVersion;
        [Key(6)] public readonly float TickDuration;
        [Key(7)] public readonly CollectorMatchData CollectorMatchData;
        [Key(8)] public readonly byte[]? ExternalGameData;
        [Key(9)] public readonly IList<InitialMatchPlayerDataGuid> PlayerData;

        public SnapshotSaverInitData(
            string snapshotSaverVersion,
            string gameName,
            string gameId,
            string gameVersion,
            int players,
            string sdkVersion,
            float tickDuration,
            CollectorMatchData collectorMatchData,
            byte[]? externalGameData,
            IList<InitialMatchPlayerDataGuid> playerData)
        {
            SnapshotSaverVersion = snapshotSaverVersion;
            GameName = gameName;
            GameId = gameId;
            GameVersion = gameVersion;
            Players = players;
            SdkVersion = sdkVersion;
            TickDuration = tickDuration;
            CollectorMatchData = collectorMatchData;
            ExternalGameData = externalGameData;
            PlayerData = playerData;
        }

        public override bool Equals(object? obj) => obj is SnapshotSaverInitData data
            && SnapshotSaverVersion == data.SnapshotSaverVersion
            && GameName == data.GameName
            && GameId == data.GameId
            && GameVersion == data.GameVersion
            && Players == data.Players
            && SdkVersion == data.SdkVersion
            && TickDuration == data.TickDuration;
        public override int GetHashCode() => HashCode.Combine(SnapshotSaverVersion, GameName, GameId, GameVersion, Players, SdkVersion, TickDuration);

        public static bool operator ==(SnapshotSaverInitData left, SnapshotSaverInitData right) => EqualityComparer<SnapshotSaverInitData>.Default.Equals(left, right);
        public static bool operator !=(SnapshotSaverInitData left, SnapshotSaverInitData right) => !(left == right);
    }
}
