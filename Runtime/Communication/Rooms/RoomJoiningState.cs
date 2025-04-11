#nullable enable

using System;

namespace Elympics
{
    internal abstract record RoomJoiningState
    {
        public record NotJoined : RoomJoiningState;

        public abstract record Joining : RoomJoiningState;
        public record Creating(string RoomName) : Joining;
        public record JoiningByRoomId(Guid RoomId) : Joining;
        public record JoiningByJoinCode(string JoinCode) : Joining;

        public abstract record Joined(Guid RoomId) : RoomJoiningState;
        public record JoinedNoTracking(Guid RoomId) : Joined(RoomId);
        public record JoinedWithTracking(Guid RoomId) : Joined(RoomId);
    }
}
