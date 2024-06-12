using System;
using System.Collections.Generic;
using System.Threading;

#nullable enable

namespace Elympics
{
    internal class RoomJoiningQueue : IRoomJoiningQueue
    {
        private readonly HashSet<Guid> _roomIds = new();
        private readonly HashSet<string> _joinCodes = new();

        private CancellationTokenSource _cts = new();

        public IDisposable AddRoomId(Guid roomId, bool ignoreExisting = false) =>
            new AutoScope(this, roomId, ignoreExisting, _cts.Token);

        public IDisposable AddJoinCode(string joinCode, bool ignoreExisting = false) =>
            new AutoScope(this, joinCode, ignoreExisting, _cts.Token);

        public void Clear()
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
            _roomIds.Clear();
            _joinCodes.Clear();
        }

        public readonly struct Data
        {
            public readonly RoomJoiningQueue Queue;
            public readonly Guid? RoomId;
            public readonly string? JoinCode;
            public readonly CancellationToken Token;

            public Data(RoomJoiningQueue queue, Guid? roomId, string? joinCode, CancellationToken token) =>
                (Queue, RoomId, JoinCode, Token) = (queue, roomId, joinCode, token);

            public void Deconstruct(out RoomJoiningQueue queue, out Guid? roomId, out string? joinCode, out CancellationToken token) =>
                (queue, roomId, joinCode, token) = (Queue, RoomId, JoinCode, Token);
        }

        public struct AutoScope : IDisposable
        {
            private Data? _data;

            public AutoScope(RoomJoiningQueue queue, Guid roomId, bool ignoreExisting, CancellationToken ct)
            {
                _data = new(queue, roomId, null, ct);
                if (!queue._roomIds.Add(roomId) && !ignoreExisting)
                    throw new RoomAlreadyJoinedException(roomId, inProgress: true);
            }

            public AutoScope(RoomJoiningQueue queue, string joinCode, bool ignoreExisting, CancellationToken ct)
            {
                _data = new(queue, null, joinCode, ct);
                if (!queue._joinCodes.Add(joinCode) && !ignoreExisting)
                    throw new RoomAlreadyJoinedException(joinCode: joinCode, inProgress: true);
            }

            public void Dispose()
            {
                if (!_data.HasValue)
                    return;
                var (queue, roomIdToRemove, joinCodeToRemove, token) = _data.Value;
                _data = null;
                if (token.IsCancellationRequested)
                    return;
                if (roomIdToRemove != null)
                    _ = queue._roomIds.Remove(roomIdToRemove.Value);
                if (joinCodeToRemove != null)
                    _ = queue._joinCodes.Remove(joinCodeToRemove);
            }
        }
    }
}
