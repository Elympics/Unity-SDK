using System;
using System.Text;

#nullable enable

namespace Elympics
{
    public class RoomAlreadyJoinedException : ElympicsException
    {
        public readonly Guid? RoomId;
        public readonly string? JoinCode;
        public readonly string? RoomName;
        public readonly bool InProgress;

        private static readonly StringBuilder Sb = new();

        internal RoomAlreadyJoinedException(Guid? roomId = null, string? joinCode = null, string? roomName = null, bool inProgress = false)
            : base(PrepareMessage(roomId, joinCode, roomName, inProgress))
        {
            RoomId = roomId;
            JoinCode = joinCode;
            RoomName = roomName;
            InProgress = inProgress;
        }

        private static string PrepareMessage(Guid? roomId, string? joinCode, string? roomName, bool inProgress)
        {
            _ = Sb.Clear()
                .Append("Only one room can be joined at once. A room");
            if (roomId != null || joinCode != null || roomName != null)
            {
                _ = Sb.Append(" identified by");
                var needsComma = false;
                var identifiers = new[]
                {
                    ("ID", roomId.ToString()),
                    ("join code", joinCode),
                    ("name", roomName),
                };
                foreach (var (identifierName, identifierValue) in identifiers)
                {
                    _ = Sb.Append($"{(needsComma ? ',' : "")} {identifierName} {identifierValue}");
                    needsComma = true;
                }
            }
            return Sb.Append(inProgress ? " is already being joined." : " has been already joined.")
                .ToString();
        }
    }
}
