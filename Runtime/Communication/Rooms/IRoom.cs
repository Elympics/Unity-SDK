using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

#nullable enable

namespace Elympics
{
    [PublicAPI]
    public interface IRoom
    {
        Guid RoomId { get; }
        RoomState State { get; }
        public sealed string RoomName => State.RoomName;
        public sealed string? JoinCode => State.JoinCode;

        bool IsDisposed { get; }
        bool IsJoined { get; }
        bool HasMatchmakingEnabled { get; }
        bool IsMatchAvailable { get; }

        UniTask UpdateRoomParams(string? roomName = null, bool? isPrivate = null, IReadOnlyDictionary<string, string>? roomCustomData = null, IReadOnlyDictionary<string, string>? customMatchmakingData = null);
        UniTask ChangeTeam(uint? teamIndex);
        public sealed UniTask BecomeSpectator() => ChangeTeam(null);

        UniTask MarkYourselfReady(byte[]? gameEngineData = null, float[]? matchmakerData = null);
        UniTask MarkYourselfUnready();

        UniTask StartMatchmaking();
        UniTask CancelMatchmaking(CancellationToken ct = default);

        /// Connects to the game server if there is a match available in the room.
        /// <remarks>The method loads the gameplay scene in the non-additive mode.</remarks>
        void PlayAvailableMatch();

        UniTask Leave();
    }
}
