using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Lobby.Models;
using MessagePack;

#nullable enable

namespace Elympics.Rooms.Models
{
    [MessagePackObject]
    public record RoomStateChanged(
        [property: Key(0)] Guid RoomId,
        [property: Key(1)] DateTime LastUpdate,
        [property: Key(2)] string RoomName,
        [property: Key(3)] string? JoinCode,
        [property: Key(4)] bool HasPrivilegedHost,
        [property: Key(5)] MatchmakingData? MatchmakingData,
        [property: Key(6)] IReadOnlyList<UserInfo> Users,
        [property: Key(7)] bool IsPrivate,
        [property: Key(8)] bool IsEphemeral,
        [property: Key(9)] IReadOnlyDictionary<string, string> CustomData) : IFromLobby
    {
        public virtual bool Equals(RoomStateChanged? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return RoomId == other.RoomId
                && LastUpdate.Equals(other.LastUpdate)
                && JoinCode == other.JoinCode
                && RoomName == other.RoomName
                && IsPrivate == other.IsPrivate
                && IsEphemeral == other.IsEphemeral
                && HasPrivilegedHost == other.HasPrivilegedHost
                && Users.SequenceEqual(other.Users)
                && Equals(MatchmakingData, other.MatchmakingData)
                && CustomData.IsTheSame(other.CustomData);
        }

        public override string ToString() => $"{nameof(RoomId)}:{RoomId}{Environment.NewLine}"
            + $"{nameof(LastUpdate)}:{LastUpdate:HH:mm:ss.ffff}{Environment.NewLine}"
            + $"{nameof(RoomName)}:{RoomName}{Environment.NewLine}"
            + $"{nameof(JoinCode)}:{JoinCode}{Environment.NewLine}"
            + $"{nameof(HasPrivilegedHost)}:{HasPrivilegedHost}{Environment.NewLine}"
            + $"{nameof(MatchmakingData)}:{Environment.NewLine}\t{MatchmakingData?.ToString().Replace(Environment.NewLine, Environment.NewLine + "\t")}{Environment.NewLine}"
            + $"{nameof(Users)}:{Environment.NewLine}\t{string.Join(Environment.NewLine + "\t", Users)}{Environment.NewLine}"
            + $"{nameof(IsPrivate)}:{IsPrivate}{Environment.NewLine}"
            + $"{nameof(IsEphemeral)}:{IsEphemeral}{Environment.NewLine}"
            + $"{nameof(CustomData)}:{Environment.NewLine}\t{string.Join(Environment.NewLine + "\t", CustomData?.Select(kv => $"Key = {kv.Value}, Value = {kv.Key}"))}{Environment.NewLine}";

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(RoomId);
            hashCode.Add(LastUpdate);
            hashCode.Add(JoinCode);
            hashCode.Add(RoomName);
            hashCode.Add(IsPrivate);
            hashCode.Add(IsEphemeral);
            hashCode.Add(HasPrivilegedHost);
            hashCode.Add(Users.Count);
            hashCode.Add(MatchmakingData);
            hashCode.Add(CustomData);
            return hashCode.ToHashCode();
        }
    }
}
