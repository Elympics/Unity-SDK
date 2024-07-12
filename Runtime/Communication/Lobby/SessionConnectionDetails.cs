using System;
using Elympics.Models.Authentication;

#nullable enable

namespace Elympics.Lobby
{
    internal readonly struct SessionConnectionDetails : IEquatable<SessionConnectionDetails>
    {
        public readonly string Url;
        public readonly AuthData? AuthData;
        public readonly Guid GameId;
        public readonly string GameVersion;
        public readonly string RegionName;
        public readonly bool CustomRegion;

        public SessionConnectionDetails(string url, AuthData? authData, Guid gameId, string gameVersion, string regionName,
            bool customRegion) =>
            (Url, AuthData, GameId, GameVersion, RegionName, CustomRegion) = (url, authData, gameId, gameVersion, regionName, customRegion);

        public void Deconstruct(out string url, out AuthData? authData, out Guid gameId, out string gameVersion, out string regionName, out bool customRegion) =>
            (url, authData, gameId, gameVersion, regionName, customRegion) = (Url, AuthData, GameId, GameVersion, RegionName, CustomRegion);

        public override string ToString() => $"{nameof(Url)}: {Url}, {nameof(AuthData.UserId)}: {AuthData?.UserId.ToString() ?? string.Empty}, {nameof(GameId)}: {GameId}, {nameof(GameVersion)}: {GameVersion}, {nameof(RegionName)}: {RegionName}";

        public bool Equals(SessionConnectionDetails other)
        {
            if (Url != other.Url)
                return false;
            if (!Equals(AuthData, other.AuthData))
                return false;
            if (GameId != other.GameId)
                return false;
            if (GameVersion != other.GameVersion)
                return false;
            if (RegionName != other.RegionName)
                return false;
            if (CustomRegion != other.CustomRegion)
                return false;
            return true;
        }
    }
}
