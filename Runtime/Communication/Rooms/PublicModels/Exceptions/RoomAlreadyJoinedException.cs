using System;
using System.Text;

#nullable enable

namespace Elympics
{
    public class RoomAlreadyJoinedException : ElympicsException
    {
        public readonly Guid? RoomId;
        public readonly string? JoinCode;
        public readonly bool InProgress;

        private static readonly StringBuilder Sb = new();

        internal RoomAlreadyJoinedException(Guid? roomId = null, string? joinCode = null, bool inProgress = false)
            : base(PrepareMessage(roomId, joinCode, inProgress))
        {
            RoomId = roomId;
            JoinCode = joinCode;
            InProgress = inProgress;
        }

        private static string PrepareMessage(Guid? roomId, string? joinCode, bool inProgress)
        {
            _ = Sb.Clear()
                .Append("Room ");
            if (roomId != null || joinCode != null)
                _ = Sb.Append("identified by ")
                    .Append(roomId != null ? $"ID {roomId} " : $"join code {joinCode} ");
            return Sb.Append(inProgress ? "is already being joined." : "has been already joined.")
                .ToString();
        }
    }
}
