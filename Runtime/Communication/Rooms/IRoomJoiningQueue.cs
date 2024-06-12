using System;

#nullable enable

namespace Elympics
{
    internal interface IRoomJoiningQueue
    {
        IDisposable AddRoomId(Guid roomId, bool ignoreExisting = false);
        IDisposable AddJoinCode(string joinCode, bool ignoreExisting = false);
        void Clear();
    }
}
