using System;

#nullable enable

namespace Elympics.Tests.Common.RoomMocks
{
    public class RoomJoiningQueueMock : IRoomJoiningQueue
    {
        public bool ShouldWorkAsWrapper { get; set; } = true;

        private readonly RoomJoiningQueue _wrappedQueue = new();

        public IDisposable AddRoomId(Guid roomId, bool ignoreExisting = false)
        {
            AddRoomIdInvoked?.Invoke((roomId, ignoreExisting));
            return ShouldWorkAsWrapper
                ? _wrappedQueue.AddRoomId(roomId, ignoreExisting)
                : SimpleDisposable.Instance;
        }

        public IDisposable AddJoinCode(string joinCode, bool ignoreExisting = false)
        {
            AddJoinCodeInvoked?.Invoke((joinCode, ignoreExisting));
            return ShouldWorkAsWrapper
                ? _wrappedQueue.AddJoinCode(joinCode, ignoreExisting)
                : SimpleDisposable.Instance;
        }

        public void Clear()
        {
            ClearInvoked?.Invoke(new ValueTuple());
            if (ShouldWorkAsWrapper)
                _wrappedQueue.Clear();
        }

        public event Action<(Guid RoomId, bool IgnoreExisting)>? AddRoomIdInvoked;
        public event Action<(string JoinCode, bool IgnoreExisting)>? AddJoinCodeInvoked;
        public event Action<ValueTuple>? ClearInvoked;

        public void Reset()
        {
            ShouldWorkAsWrapper = true;
            _wrappedQueue.Clear();
            AddRoomIdInvoked = null;
            AddJoinCodeInvoked = null;
            ClearInvoked = null;
        }

        private class SimpleDisposable : IDisposable
        {
            public static readonly SimpleDisposable Instance = new();

            private SimpleDisposable()
            { }

            public void Dispose()
            { }
        }
    }
}
