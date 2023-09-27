using System;
using Elympics.Models.Authentication;

#nullable enable

namespace Elympics.Lobby
{
    internal readonly struct SessionConnectionDetails
    {
        public readonly string Url;
        public readonly AuthData AuthData;
        public readonly Guid GameId;
        public readonly string GameVersion;
        public readonly string RegionName;

        public SessionConnectionDetails(string url, AuthData authData, Guid gameId, string gameVersion, string regionName) =>
            (Url, AuthData, GameId, GameVersion, RegionName) = (url, authData, gameId, gameVersion, regionName);

        public void Deconstruct(out string url, out AuthData authData, out Guid gameId, out string gameVersion, out string regionName) =>
            (url, authData, gameId, gameVersion, regionName) = (Url, AuthData, GameId, GameVersion, RegionName);

        public override string ToString() => $"{nameof(Url)}: {Url}, {nameof(AuthData.UserId)}: {AuthData.UserId}, {nameof(GameId)}: {GameId}, {nameof(GameVersion)}: {GameVersion}, {nameof(RegionName)}: {RegionName}";
    }
}
