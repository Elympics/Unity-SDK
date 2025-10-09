using System;
using System.Collections.Generic;
using System.Linq;
using Elympics.Rooms.Models;
using MessagePack;

#nullable enable

namespace Elympics.Communication.Rooms.InternalModels
{
    [MessagePackObject]
    public record PublicRoomState(
        [property: Key(0)] Guid RoomId,
        [property: Key(1)] DateTime LastUpdate,
        [property: Key(2)] string RoomName,
        [property: Key(3)] bool HasPrivilegedHost,
        [property: Key(4)] PublicMatchmakingData? MatchmakingData,
        [property: Key(5)] IReadOnlyList<UserInfoDto> Users,
        [property: Key(6)] bool IsPrivate,
        [property: Key(7)] IReadOnlyDictionary<string, string> CustomData)
    {
        public virtual bool Equals(PublicRoomState? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return RoomId == other.RoomId
                && RoomName == other.RoomName
                && LastUpdate == other.LastUpdate
                && HasPrivilegedHost == other.HasPrivilegedHost
                && IsPrivate == other.IsPrivate
                && Users.SequenceEqual(other.Users)
                && Equals(MatchmakingData, other.MatchmakingData)
                && CustomData.IsTheSame(other.CustomData);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(RoomId);
            hashCode.Add(RoomName);
            hashCode.Add(LastUpdate);
            hashCode.Add(HasPrivilegedHost);
            hashCode.Add(IsPrivate);
            hashCode.Add(Users.Count);
            hashCode.Add(MatchmakingData);
            hashCode.Add(CustomData);
            return hashCode.ToHashCode();
        }

        public bool ContainsUser(Guid userId) => Users.Any(user => Guid.Parse(user.User.userId) == userId);
    }
}
